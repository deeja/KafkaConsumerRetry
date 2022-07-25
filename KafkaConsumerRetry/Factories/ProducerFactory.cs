using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories {
    public class ProducerFactory : IProducerFactory {
        private readonly RetryServiceConfig _config;

        public ProducerFactory(RetryServiceConfig config) {
            _config = config;
        }

        public IProducer<byte[], byte[]> BuildRetryProducer() {
            var producerConfig = new ProducerConfig(_config.RetryKafka ?? _config.TopicKafka);
            return new ProducerBuilder<byte[], byte[]>(producerConfig).Build();
        }
    }
}