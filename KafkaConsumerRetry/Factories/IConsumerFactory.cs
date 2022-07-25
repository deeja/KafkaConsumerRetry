using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories {
    public interface IConsumerFactory {
        IConsumer<byte[], byte[]> BuildOriginConsumer();
        IConsumer<byte[], byte[]> BuildRetryConsumer();
    }
}