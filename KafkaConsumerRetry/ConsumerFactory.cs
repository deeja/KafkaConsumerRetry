using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry {
    public class ConsumerFactory : IConsumerFactory {
        private readonly RetryServiceConfig _config;
        public ConsumerFactory(RetryServiceConfig config) {
            _config = config;
        }

        public IConsumer<byte[], byte[]> BuildOriginConsumer() {
            return new ConsumerBuilder<byte[], byte[]>(_config.TopicKafka).Build();
        }

        public IConsumer<byte[], byte[]> BuildRetryConsumer() {
            return new ConsumerBuilder<byte[], byte[]>(_config.RetryKafka ?? _config.TopicKafka).Build();
        }
    }
}