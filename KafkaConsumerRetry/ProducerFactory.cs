using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry {
    public class ProducerFactory : IProducerFactory {
        private readonly RetryServiceConfig _config;

        public ProducerFactory(RetryServiceConfig config) {
            _config = config;
        }

        public IProducer<byte[], byte[]> BuildRetryProducer() {
            return new ProducerBuilder<byte[], byte[]>(_config.RetryKafka ?? _config.TopicKafka).Build();
        }
    }
}