using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public interface IConsumerFactory {
    IConsumer<byte[], byte[]> BuildOriginConsumer(KafkaRetryConfig config, TopicNaming naming);
    IConsumer<byte[], byte[]> BuildRetryConsumer(KafkaRetryConfig config, TopicNaming naming);
}