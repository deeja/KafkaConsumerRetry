using Confluent.Kafka;

namespace KafkaConsumerRetry {
    public interface IConsumerFactory {
        IConsumer<byte[], byte[]> BuildOriginConsumer();
        IConsumer<byte[], byte[]> BuildRetryConsumer();
    }
}