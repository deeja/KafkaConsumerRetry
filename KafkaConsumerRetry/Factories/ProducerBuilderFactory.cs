using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public class ProducerBuilderFactory : IProducerBuilderFactory {
    public ProducerBuilder<byte[], byte[]> CreateProducerBuilder(ProducerConfig config) {
        return new ProducerBuilder<byte[], byte[]>(config);
    }
}