using System.Collections.Concurrent;
using System.Text;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Rolls through queued messages, working on them and pushing them to the next retry if there is a failure
/// </summary>
internal class PartitionProcessor : IDisposable {
    private const string LastExceptionKey = "LAST_EXCEPTION";

    private const int PauseThreshold = 5;
    private const string RetryConsumerGroupIdKey = "RETRY_GROUP_ID";
    private readonly IConsumer<byte[], byte[]> _consumer;

    /// <summary>
    ///     Tasks that are to be run by this context
    /// </summary>
    private readonly ConcurrentQueue<ConsumeResult<byte[], byte[]>> _consumeResultQueue = new();

    private readonly IConsumerResultHandler _consumerResultHandler;
    private readonly Task _coreTask;
    private readonly IDelayCalculator _delayCalculator;
    private readonly ILogger<PartitionProcessor> _logger;

    private readonly string _nextTopic;
    private readonly IRateLimiter _rateLimiter;
    private readonly string _retryGroupId;
    private readonly int _retryIndex;
    private readonly IProducer<byte[], byte[]> _retryProducer;
    private readonly TopicPartition _topicPartition;
    private readonly CancellationTokenSource _workerTokenSource;

    private bool _isPaused;
    private bool _revoked;

    public PartitionProcessor(IConsumerResultHandler consumerResultHandler,
        ILoggerFactory factory,
        IDelayCalculator delayCalculator,
        IRateLimiter rateLimiter,
        IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition,
        IProducer<byte[], byte[]> retryProducer, string retryGroupId, string nextTopic, int retryIndex) {
        _consumerResultHandler = consumerResultHandler;
        _delayCalculator = delayCalculator;
        _rateLimiter = rateLimiter;
        _workerTokenSource = new CancellationTokenSource();
        _consumer = consumer;
        _topicPartition = topicPartition;
        _retryProducer = retryProducer;
        _retryGroupId = retryGroupId;
        _nextTopic = nextTopic;
        _retryIndex = retryIndex;
        // TODO: Tie cancellation to the host applications lifetime
        _logger = factory.CreateLogger<PartitionProcessor>();
        _coreTask = DoWorkAsync();
    }

    public void Dispose() {
        // Do not dispose the consumer or the producer
        _workerTokenSource.Dispose();
    }

    /// <summary>
    ///     Process items in the queue for this specific topic partition
    /// </summary>
    private async Task DoWorkAsync() {
        await Task.Yield();
        var cancellationToken = _workerTokenSource.Token;
        while (!(cancellationToken.IsCancellationRequested || _revoked)) {
            if (_consumeResultQueue.TryDequeue(out var consumeResult)) {
                _logger.LogTrace("[{TopicPartition}] - Dequeued {Key}", _topicPartition, consumeResult.Message.Key);
                try {
                    var delayTime = _delayCalculator.Calculate(consumeResult, _retryIndex);
                    await Task.Delay(delayTime, cancellationToken);
                    await _rateLimiter.WaitAsync(cancellationToken);
                    if (cancellationToken.IsCancellationRequested) {
                        break;
                    }

                    await _consumerResultHandler.HandleAsync(consumeResult, cancellationToken);
                    _consumer.StoreOffset(consumeResult); // don't put outside the loop due to the break
                }
                catch (Exception handledException) {
                    _logger.LogError(handledException,
                        "Failed to process message. Pushing to next topic: `{RetryQueue}`. Message Key: `{MessageKey}`",
                        _nextTopic, consumeResult.Message.Key);
                    AppendException(handledException, _retryGroupId, consumeResult.Message);
                    SetTimestamp(consumeResult);
                    await _retryProducer.ProduceAsync(_nextTopic, consumeResult.Message,
                        cancellationToken);
                    _consumer.StoreOffset(consumeResult); // need to set after handing off to retry
                }
                finally {
                    _rateLimiter.Release();
                }
            }
            else {
                if (_isPaused) {
                    _consumer.Resume(new[] { _topicPartition });
                    _isPaused = false;
                }

                await Task.Delay(100, cancellationToken);
                // TODO: semaphore or something here rather than a delay
            }
        }
    }

    private void SetTimestamp(ConsumeResult<byte[], byte[]> consumeResult) {
        consumeResult.Message.Timestamp = new Timestamp(DateTimeOffset.UtcNow);
    }

    private void AppendException(Exception exception, string retryConsumerGroupId,
        Message<byte[], byte[]> consumeMessage) {
        consumeMessage.Headers.Remove(LastExceptionKey);
        consumeMessage.Headers.Add(LastExceptionKey, Encoding.UTF8.GetBytes(exception.ToString()));
        consumeMessage.Headers.Remove(RetryConsumerGroupIdKey);
        consumeMessage.Headers.Add(RetryConsumerGroupIdKey, Encoding.UTF8.GetBytes(retryConsumerGroupId));
    }

    /// <summary>
    /// </summary>
    /// <remarks>
    ///     This code runs per topic partition, so access is assumed to be sequential (unlikely to have multiple threads)
    /// </remarks>
    /// <param name="consumeResult"></param>
    public void Enqueue(ConsumeResult<byte[], byte[]> consumeResult) {
        _consumeResultQueue.Enqueue(consumeResult);

        // if there are more in the queue than there should be, then pause the partition to halt the pulling of those messages
        if (_consumeResultQueue.Count <= PauseThreshold) {
            return;
        }

        _consumer.Pause(new[] { _topicPartition });
        _logger.LogTrace("PAUSING self: {TopicPartition}", _topicPartition);
        _isPaused = true;
    }

    public void Cancel() {
        _logger.LogTrace("CANCELLING self: {TopicPartition}", _topicPartition);
        _workerTokenSource.Cancel();
    }

    public async Task RevokeAsync() {
        _logger.LogTrace("REVOKING self: {TopicPartition}", _topicPartition);
        _revoked = true;
        await _coreTask;
    }
}