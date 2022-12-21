using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Exceptions;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Assigned per Partition, this class processes queued messages for that partition.
///     Processing failures are pushed to the next retry topic.
/// </summary>
/// <remarks>
///     The top inheriting class should be sealed. This helps with performance significantly due to the number of virtual
///     methods.
/// </remarks>
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public abstract class PartitionProcessorBase : IDisposable, IPartitionProcessor {
    private protected const string LastExceptionKey = "KCR_LAST_EXCEPTION";
    private protected const string RetryConsumerGroupIdKey = "KCR_RETRY_GROUP_ID";
    private protected readonly IConsumer<byte[], byte[]> _consumer;

    /// <summary>
    ///     Tasks that are to be run by this context
    /// </summary>
    private protected readonly ConcurrentQueue<(Type Handler, ConsumeResult<byte[], byte[]> ConsumeResult)> _consumeResultQueue = new();

    private protected readonly IDelayCalculator _delayCalculator;
    private protected readonly ILogger _logger;
    private protected readonly string _nextTopic;

    private protected readonly int _queueSizePauseThreshold;
    private protected readonly IRateLimiter _rateLimiter;
    private protected readonly string _retryGroupId;
    private protected readonly int _retryIndex;
    private protected readonly IProducer<byte[], byte[]> _retryProducer;
    private protected readonly IServiceProvider _serviceProvider;

    private protected readonly object _startLock = new();
    private protected readonly TopicPartition _topicPartition;
    private protected readonly CancellationTokenSource _workerTokenSource;

    private protected Task? _coreTask;
    private protected bool _isPaused;
    private protected bool _partitionRevoked;

    /// <summary>
    ///     <see cref="PartitionProcessorBase" /> processors messages placed on its queue. It is assigned to a particular
    ///     partition and handles the allocation, loss and revoking events that affect that partition.
    /// </summary>
    /// <param name="logger">Logger that should be typed to the top class</param>
    /// <param name="serviceProvider">Used to create scopes if needed</param>
    /// <param name="delayCalculator">Calculator used to determine the delay between retries</param>
    /// <param name="rateLimiter">Restricts the number of messages being processed globally</param>
    /// <param name="consumer">Consumer that received the message</param>
    /// <param name="topicPartition">Partition information</param>
    /// <param name="retryProducer">Producer for pushing to the next retry queue if needed</param>
    /// <param name="retryGroupId">Group Id of this consumer</param>
    /// <param name="nextTopic">Retry Topic that will be pushed to on failure </param>
    /// <param name="retryIndex">Current retry index; 0 = origin queue, 1 = first retry</param>
    /// <param name="queueSizePauseThreshold">
    ///     Message count in the queue before the partition is paused. Note: A large count
    ///     increases memory usage, but is better for fast running tasks
    /// </param>
    protected PartitionProcessorBase(ILogger logger,
        IServiceProvider serviceProvider,
        IDelayCalculator delayCalculator,
        IRateLimiter rateLimiter,
        IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition,
        IProducer<byte[], byte[]> retryProducer, string retryGroupId, string nextTopic, int retryIndex, int queueSizePauseThreshold = 20) {
        _serviceProvider = serviceProvider;
        _delayCalculator = delayCalculator;
        _queueSizePauseThreshold = queueSizePauseThreshold;
        _rateLimiter = rateLimiter;
        _workerTokenSource = new CancellationTokenSource();
        _consumer = consumer;
        _topicPartition = topicPartition;
        _retryProducer = retryProducer;
        _retryGroupId = retryGroupId;
        _nextTopic = nextTopic;
        _retryIndex = retryIndex;
        _logger = logger;
    }

    public void Dispose() {
        // Do not dispose the consumer or the producer
        _workerTokenSource.Dispose();
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    ///     This code runs per topic partition, so access is assumed to be sequential (unlikely to have multiple threads)
    /// </remarks>
    /// <param name="consumeResult"></param>
    public virtual void Enqueue<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult) where TResultHandler : IConsumerResultHandler {
        _consumeResultQueue.Enqueue((typeof(TResultHandler), consumeResult));
        
        // if there are more in the queue than there should be, then pause the partition to halt the pulling of those messages
        if (_consumeResultQueue.Count <= _queueSizePauseThreshold) {
            return;
        }

        _consumer.Pause(new[] { _topicPartition });
        _logger.LogTrace("PAUSING self: {TopicPartition}", _topicPartition);
        _isPaused = true;
    }

    public virtual void Cancel() {
        _logger.LogTrace("CANCELLING self: {TopicPartition}", _topicPartition);
        _workerTokenSource.Cancel();
    }

    public virtual async Task RevokeAsync() {
        _logger.LogTrace("REVOKING self: {TopicPartition}", _topicPartition);
        _partitionRevoked = true;
        if (_coreTask != null) {
            await _coreTask;
        }
    }

    public virtual void Start() {
        lock (_startLock) {
            if (_coreTask is { }) {
                // throwing an exception as something isn't right if this is called twice
                throw new CoreTaskAlreadyStartedException();
            }

            _coreTask = DoWorkAsync();
        }
    }

    /// <summary>
    ///     Process items in the queue for this specific topic partition
    /// </summary>
    protected virtual async Task DoWorkAsync() {
        await Task.Yield();
        var cancellationToken = _workerTokenSource.Token;
        while (CanContinue(cancellationToken)) {
            if (_consumeResultQueue.TryDequeue(out var tuple)) {
                await ProcessDequeued(tuple, cancellationToken);
            }
            else {
                // Delay Polling worked out as a better option than Reset Events.
                // Reset events can get missed when a message event comes in before the event/semaphore is waited.
                // Adding a timeout timespan means it's basically delay polling anyway 
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
            }

            if (_isPaused && CanContinue(cancellationToken)) {
                _consumer.Resume(new[] { _topicPartition });
                _isPaused = false;
            }
        }
    }

    protected virtual async Task ProcessDequeued((Type Handler, ConsumeResult<byte[], byte[]> ConsumeResult) tuple, CancellationToken cancellationToken) {
        var consumeResult = tuple.ConsumeResult;
        _logger.LogTrace("[{TopicPartition}] - Dequeued {Key}", _topicPartition, consumeResult.Message.Key);
        try {
            if (await HandleMessage(consumeResult, cancellationToken, tuple)) {
                _consumer.StoreOffset(consumeResult);
            }
        }
        catch (Exception handledException) {
            await HandleException(handledException, consumeResult, cancellationToken);
            _consumer.StoreOffset(consumeResult); // need to set after handing off to retry
        }
    }

    protected virtual bool CanContinue(CancellationToken cancellationToken) {
        return !(cancellationToken.IsCancellationRequested || _partitionRevoked);
    }

    protected virtual async Task HandleException(Exception handledException, ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        _logger.LogError(handledException,
            "Failed to process message. Pushing to next topic: `{RetryQueue}`. Message Key: `{MessageKey}`",
            _nextTopic, consumeResult.Message.Key);
        AppendException(handledException, _retryGroupId, consumeResult.Message);
        SetTimestamp(consumeResult);
        await PushToRetry(consumeResult, cancellationToken);
    }

    protected virtual async Task PushToRetry(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        var deliveryResult = await _retryProducer.ProduceAsync(_nextTopic, consumeResult.Message, cancellationToken);
        await HandleRetryDeliveryResultAsync(deliveryResult, cancellationToken);
    }

    /// <summary>
    ///     Exposes the delivery result when pushing to the retry queue
    /// </summary>
    /// <param name="deliveryResult">Retry delivery result</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if handled</returns>
    protected virtual Task HandleRetryDeliveryResultAsync(DeliveryResult<byte[], byte[]> deliveryResult, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }

    protected virtual async Task<bool> HandleMessage(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken,
        (Type Handler, ConsumeResult<byte[], byte[]> ConsumeResult) tuple) {
        var delayTime = _delayCalculator.Calculate(consumeResult, _retryIndex);

        if (delayTime > DateTimeOffset.Now) {
            await DelayTillRetryTime(delayTime, cancellationToken);
        }

        await _rateLimiter.WaitAsync(cancellationToken);

        try {
            if (cancellationToken.IsCancellationRequested) {
                return false;
            }

            await HandleMessage(tuple.Handler, consumeResult, cancellationToken);

            return true;
        }
        finally {
            _rateLimiter.Release();
        }
    }

    private async static Task DelayTillRetryTime(DateTimeOffset delayTime, CancellationToken cancellationToken) {
        var delayTimespan = delayTime - DateTimeOffset.Now;
        var delayMs = Math.Max(delayTimespan.Milliseconds, 0);
        await Task.Delay(delayMs, cancellationToken);
    }

    protected virtual async Task HandleMessage(Type handlerType, ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        using var serviceScope = _serviceProvider.CreateScope();
        var handler = (IConsumerResultHandler)serviceScope.ServiceProvider.GetRequiredService(handlerType);
        await handler.HandleAsync(consumeResult, cancellationToken);
    }

    protected virtual void SetTimestamp(ConsumeResult<byte[], byte[]> consumeResult) {
        consumeResult.Message.Timestamp = new Timestamp(DateTimeOffset.UtcNow);
    }

    protected virtual void AppendException(Exception exception, string retryConsumerGroupId,
        Message<byte[], byte[]> consumeMessage) {
        consumeMessage.Headers.Remove(LastExceptionKey);
        consumeMessage.Headers.Add(LastExceptionKey, Encoding.UTF8.GetBytes(exception.ToString()));
        consumeMessage.Headers.Remove(RetryConsumerGroupIdKey);
        consumeMessage.Headers.Add(RetryConsumerGroupIdKey, Encoding.UTF8.GetBytes(retryConsumerGroupId));
    }
}