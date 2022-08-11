using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public interface IProducerFactory {
    IProducer<byte[], byte[]> BuildRetryProducer(ProducerConfig config);
}