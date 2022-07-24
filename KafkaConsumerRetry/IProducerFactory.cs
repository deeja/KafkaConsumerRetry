using Confluent.Kafka;

namespace KafkaConsumerRetry {
    public interface IProducerFactory {
        IProducer<byte[], byte[]> BuildRetryProducer();
    }
}