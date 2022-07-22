﻿using System.Collections.Generic;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    public class TopicPartitionQueueAllocator : ITopicPartitionQueueAllocator {
        private readonly IMessageValueHandler _handler;
        private readonly Dictionary<TopicPartition, TopicPartitionQueue> _partitionQueues = new();
        private readonly IProducer<byte[], byte[]> _producer;
        private readonly IDelayCalculator _delayCalculator;

        public TopicPartitionQueueAllocator(IMessageValueHandler handler, IProducer<byte[], byte[]> producer, IDelayCalculator delayCalculator) {
            _handler = handler;
            _producer = producer;
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