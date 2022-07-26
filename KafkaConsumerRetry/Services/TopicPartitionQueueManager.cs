using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services {
    public class TopicPartitionQueueManager : ITopicPartitionQueueManager {
        private readonly ILimiter _limiter;
        private readonly IDelayCalculator _delayCalculator;
        private readonly IConsumerResultHandler _handler;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<TopicPartition, TopicPartitionQueue> _partitionQueues = new();
        private readonly IProducer<byte[], byte[]> _producer;

        public TopicPartitionQueueManager(IConsumerResultHandler handler, IProducerFactory producer,
            IDelayCalculator delayCalculator, ILoggerFactory loggerFactory, ILimiter limiter) {
            _producer = producer.BuildRetryProducer();
            _handler = handler;
            _delayCalculator = delayCalculator;
            _loggerFactory = loggerFactory;
            _limiter = limiter;
        }

        public void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult) {
            var topicPartition = consumeResult.TopicPartition;

            var partitionQueue = _partitionQueues[topicPartition];

            partitionQueue.Enqueue(consumeResult);
        }

        public void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
            foreach (var topicPartitionOffset in list) {
                _partitionQueues[topicPartitionOffset.TopicPartition].Cancel();
                _partitionQueues.Remove(topicPartitionOffset.TopicPartition);
            }
        }

        public void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartition> list) {
            foreach (var topicPartition in list) {
                _partitionQueues.Add(topicPartition,
                    new TopicPartitionQueue(_handler, _loggerFactory, _delayCalculator, _limiter, consumer,
                        topicPartition,
                        _producer, retryGroupId, nextTopic, retryIndex));
            }
        }

        public void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
            List<Task> revokeWaiters = new();
            foreach (var topicPartitionOffset in list) {
                var topicPartition = topicPartitionOffset.TopicPartition;
                var partitionQueue = _partitionQueues[topicPartition];
                _partitionQueues.Remove(topicPartition);
                revokeWaiters.Add(partitionQueue.RevokeAsync());
            }

            // TODO: Something better than this waiter
            Task.Run(() => Task.WaitAll(revokeWaiters.ToArray())).GetAwaiter().GetResult();
            consumer.Commit();
        }
    }
}