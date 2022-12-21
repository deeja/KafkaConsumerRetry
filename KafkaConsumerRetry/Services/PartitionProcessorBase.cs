using System.Collections.Concurrent;
using System.Text;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Rolls through queued messages, working on them and pushing them to the next retry if there is a failure
/// </summary>
public abstract class PartitionProcessorBase : IDisposable, IPartitionProcessor {
    private const string LastExceptionKey = "LAST_EXCEPTION";

    private const int PauseThreshold = 20;
    private const string RetryConsumerGroupIdKey = "RETRY_GROUP_ID";
    private readonly IConsumer<byte[], byte[]> _consumer;

    /// <summary>
    ///     Tasks that are to be run by this context
    /// </summary>
    private readonly ConcurrentQueue<(Type Handler, ConsumeResult<byte[], byte[]> ConsumeResult)> _consumeResultQueue = new();

    private Task _coreTask;
    private readonly IDelayCalculator _delayCalculator;
    private readonly ILogger _logger;

    private readonly string _nextTopic;
    private readonly IRateLimiter _rateLimiter;
    private readonly string _retryGroupId;
    private readonly int _retryIndex;
    private readonly IProducer<byte[], byte[]> _retryProducer;
    private readonly IServiceProvider _serviceProvider;
    private readonly TopicPartition _topicPartition;
    private readonly CancellationTokenSource _workerTokenSource;

    private bool _isPaused;
    private bool _partitionRevoked;

    protected PartitionProcessorBase(ILogger logger,
        IServiceProvider serviceProvider,
        IDelayCalculator delayCalculator,
        IRateLimiter rateLimiter,
        IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition,
        IProducer<byte[], byte[]> retryProducer, string retryGroupId, string nextTopic, int retryIndex) {
        _serviceProvider = serviceProvider;
        _delayCalculator = delayCalculator;
        _rateLimiter = rateLimiter;
        _workerTokenSource = new CancellationTokenSource();
        _consumer = consumer;
        _topicPartition = topicPartition;
        _retryProducer = retryProducer;
        _retryGroupId = retryGroupId;
        _nextTopic = nextTopic;
        _retryIndex = retryIndex;
        _logger = logger;

        _coreTask = DoWorkAsync();
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
        if (_consumeResultQueue.Count <= PauseThreshold) {
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
        await _coreTask;
    }


    public virtual void Start() {
        _coreTask = DoWorkAsync();
    }

    /// <summary>
    ///     Process items in the queue for this specific topic partition
    /// </summary> 
    protected virtual async Task DoWorkAsync() {
        await Task.Yield();
        var cancellationToken = _workerTokenSource.Token;
        while (CanContinue(cancellationToken)) {
            if (_consumeResultQueue.TryDequeue(out var tuple)) {
                var consumeResult = tuple.ConsumeResult;
                _logger.LogTrace("[{TopicPartition}] - Dequeued {Key}", _topicPartition, consumeResult.Message.Key);
                try {
                    var getNext = await CallHandler(consumeResult, cancellationToken, tuple);
                    if (!getNext) {
                        break;
                    }
                }
                catch (Exception handledException) {
                    await HandleException(handledException, consumeResult, cancellationToken);
                }
                finally {
                    // _rateLimiter used inside CallHandler
                    _rateLimiter.Release();
                }
            }
            else {
                await Task.Delay(100, cancellationToken);
                // TODO: semaphore or something here rather than a delay on queue miss
            }

            // if is paused, then resume as a message has been added
            if (_isPaused) {
                _consumer.Resume(new[] { _topicPartition });
                _isPaused = false;
            }
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
        await _retryProducer.ProduceAsync(_nextTopic, consumeResult.Message, cancellationToken);
        _consumer.StoreOffset(consumeResult); // need to set after handing off to retry
    }

    protected virtual async Task<bool> CallHandler(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken,
        (Type Handler, ConsumeResult<byte[], byte[]> ConsumeResult) tuple) {
        var delayTime = _delayCalculator.Calculate(consumeResult, _retryIndex);

        if (delayTime > DateTimeOffset.Now) {
            var delayTimespan = delayTime - DateTimeOffset.Now;
            var delayMs = Math.Max(delayTimespan.Milliseconds, 0);
            await Task.Delay(delayMs, cancellationToken);
        }

        await _rateLimiter.WaitAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested) {
            return false;
        }

        using (var serviceScope = _serviceProvider.CreateScope()) {
            var handler = (IConsumerResultHandler)serviceScope.ServiceProvider.GetRequiredService(tuple.Handler);
            await handler.HandleAsync(consumeResult, cancellationToken);
        }

        _consumer.StoreOffset(consumeResult); // don't put outside the loop due to the break
        return true;
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