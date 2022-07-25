using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services {
    /// <summary>
    ///     Rolls through queued messages, working on them and pushing them to the next retry if there is a failure
    /// </summary>
    internal class TopicPartitionQueue : IDisposable {
        private readonly IConsumer<byte[], byte[]> _consumer;

        /// <summary>
        ///     Tasks that are to be run by this context
        /// </summary>
        private readonly ConcurrentQueue<ConsumeResult<byte[], byte[]>> _consumeResultQueue = new();

        private readonly IConsumerResultHandler _consumerResultHandler;
        private readonly IDelayCalculator _delayCalculator;
        private readonly ILogger<TopicPartitionQueue> _logger;

        private readonly string _nextTopic;
        private readonly string _retryGroupId;
        private readonly int _retryIndex;
        private readonly IProducer<byte[], byte[]> _retryProducer;
        private readonly TopicPartition _topicPartition;
        private readonly CancellationTokenSource _workerTokenSource;
        private readonly string LastExceptionKey = "LAST_EXCEPTION";

        private readonly int PAUSE_THRESHOLD = 5;
        private readonly string RetryConsumerGroupIdKey = "RETRY_GROUP_ID";

        private bool _isPaused;

        public TopicPartitionQueue(IConsumerResultHandler consumerResultHandler,
            ILoggerFactory factory,
            IDelayCalculator delayCalculator,
            IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition,
            IProducer<byte[], byte[]> retryProducer, string retryGroupId, string nextTopic, int retryIndex) {
            _consumerResultHandler = consumerResultHandler;
            _delayCalculator = delayCalculator;
            _workerTokenSource = new CancellationTokenSource();
            _consumer = consumer;
            _topicPartition = topicPartition;
            _retryProducer = retryProducer;
            _retryGroupId = retryGroupId;
            _nextTopic = nextTopic;
            _retryIndex = retryIndex;
            // TODO: Tie cancellation to the host applications lifetime
            _logger = factory.CreateLogger<TopicPartitionQueue>();

            _ = Task.Factory.StartNew(async () => await DoWorkAsync(), _workerTokenSource.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Dispose() {
            // Do not dispose the consumer or the producer
            _workerTokenSource.Dispose();
        }

        /// <summary>
        ///     Process items in the queue for this specific topic partition
        /// </summary>
        private async Task DoWorkAsync() {
            var cancellationToken = _workerTokenSource.Token;
            while (!cancellationToken.IsCancellationRequested) {
                if (_consumeResultQueue.TryDequeue(out var consumeResult)) {
                    try {
                        var delayTime = _delayCalculator.Calculate(consumeResult, _retryIndex);
                        await Task.Delay(delayTime, cancellationToken);
                        await _consumerResultHandler.HandleAsync(consumeResult, cancellationToken);
                    }
                    catch (Exception handledException) {
                        _logger.LogError(handledException,
                            "Failed to process message. Placing in retry. Message Key: {MessageKey}",
                            consumeResult.Message.Key);
                        AppendException(handledException, _retryGroupId, consumeResult.Message);
                        await _retryProducer.ProduceAsync(_nextTopic, consumeResult.Message,
                            cancellationToken);
                    }

                    _consumer.StoreOffset(consumeResult);
                }
                else {
                    if (_isPaused) {
                        _consumer.Resume(new[] {_topicPartition});
                        _isPaused = false;
                    }

                    await Task.Delay(100, cancellationToken);
                    // TODO: semaphore or something here rather than a delay
                }
            }
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
            if (_consumeResultQueue.Count > PAUSE_THRESHOLD) {
                _consumer.Pause(new[] {_topicPartition});
                _isPaused = true;
            }
        }

        /// <summary>
        ///     Call when the topic partition has been revoked
        /// </summary>
        public void Cancel() {
            _workerTokenSource.Cancel();
        }
    }
}