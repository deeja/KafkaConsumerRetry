using System.Collections.Generic;
using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services {
    public class TopicPartitionQueueManager : ITopicPartitionQueueManager {
        private readonly IDelayCalculator _delayCalculator;
        private readonly IConsumerResultHandler _handler;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dictionary<TopicPartition, TopicPartitionQueue> _partitionQueues = new();
        private readonly IProducer<byte[], byte[]> _producer;

        public TopicPartitionQueueManager(IConsumerResultHandler handler, IProducerFactory producer,
            IDelayCalculator delayCalculator, ILoggerFactory loggerFactory) {
            _handler = handler;
            _producer = producer.BuildRetryProducer();
            _delayCalculator = delayCalculator;
            _loggerFactory = loggerFactory;
        }

        public void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult, IConsumer<byte[], byte[]> consumer,
            string retryGroupId, string nextTopic, int retryIndex) {
            var topicPartition = consumeResult.TopicPartition;
            if (!_partitionQueues.ContainsKey(topicPartition)) {
                _partitionQueues.Add(topicPartition,
                    new TopicPartitionQueue(_handler, _loggerFactory, _delayCalculator, consumer, topicPartition,
                        _producer, retryGroupId, nextTopic, retryIndex));
            }

            var partitionQueue = _partitionQueues[topicPartition];

            partitionQueue.Enqueue(consumeResult);
        }
    }
}