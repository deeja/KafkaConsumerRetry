using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories {
    public class ConsumerFactory : IConsumerFactory {
        private readonly RetryServiceConfig _config;

        public ConsumerFactory(RetryServiceConfig config) {
            _config = config;
        }

        public IConsumer<byte[], byte[]> BuildOriginConsumer() {
            var consumerConfig = new ConsumerConfig(_config.TopicKafka);
            return new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build();
        }

        public IConsumer<byte[], byte[]> BuildRetryConsumer() {
            var consumerConfig = new ConsumerConfig(_config.RetryKafka ?? _config.TopicKafka);
            return new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build();
        }
    }
}