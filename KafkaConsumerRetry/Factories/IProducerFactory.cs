using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public interface IProducerFactory {
    IProducer<byte[], byte[]> BuildRetryProducer(ProducerConfig config);
}