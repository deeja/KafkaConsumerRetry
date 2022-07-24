using System.Collections.Generic;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    public class TopicPartitionQueueManager : ITopicPartitionQueueManager {
        private readonly IConsumerResultHandler _handler;
        private readonly Dictionary<TopicPartition, TopicPartitionQueue> _partitionQueues = new();
        private readonly IProducer<byte[], byte[]> _producer;
        private readonly IDelayCalculator _delayCalculator;

        public TopicPartitionQueueManager(IConsumerResultHandler handler, IProducerFactory producer, IDelayCalculator delayCalculator) {
            _handler = handler;
            _producer = producer.BuildRetryProducer();
            _delayCalculator = delayCalculator;
        }

        public void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult, IConsumer<byte[], byte[]> consumer,
            string retryGroupId, string nextTopic, int retryIndex) {
            var topicPartition = consumeResult.TopicPartition;
            if (!_partitionQueues.ContainsKey(topicPartition)) {
                _partitionQueues.Add(topicPartition,
                    new TopicPartitionQueue(_handler, _delayCalculator, consumer, topicPartition, _producer, retryGroupId, nextTopic, retryIndex));
            }

            var partitionQueue = _partitionQueues[topicPartition];

            partitionQueue.Enqueue(consumeResult);
        }
    }
}