using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public interface IProducerBuilderFactory {
    ProducerBuilder<byte[], byte[]> CreateProducerBuilder(ProducerConfig config);
}