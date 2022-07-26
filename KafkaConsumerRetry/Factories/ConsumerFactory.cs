using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Factories {
    public class ConsumerFactory : IConsumerFactory {
        private readonly RetryServiceConfig _config;
        private readonly ITopicPartitionQueueManager _queueManager;

        public ConsumerFactory(RetryServiceConfig config, ITopicPartitionQueueManager queueManager) {
            _config = config;
            _queueManager = queueManager;
        }

        public IConsumer<byte[], byte[]> BuildOriginConsumer() {
            var consumerConfig = new ConsumerConfig(_config.TopicKafka);
            var consumerBuilder = new ConsumerBuilder<byte[], byte[]>(consumerConfig);

            // (Incremental balancing) Assign/Unassign must not be called in the handler.
            consumerBuilder.SetPartitionsLostHandler((consumer, list) => _queueManager.HandleLostPartitions(consumer, list));
            consumerBuilder.SetPartitionsAssignedHandler((consumer, list) => _queueManager.HandleAssignedPartitions(consumer, list));
            consumerBuilder.SetPartitionsRevokedHandler((consumer, list) => _queueManager.HandleRevokedPartitions(consumer, list));
            return consumerBuilder.Build();
        }

        public IConsumer<byte[], byte[]> BuildRetryConsumer() {
            var consumerConfig = new ConsumerConfig(_config.RetryKafka ?? _config.TopicKafka);
            return new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build();
        }
    }
}