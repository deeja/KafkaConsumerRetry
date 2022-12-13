using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public interface IConsumerFactory {
    IConsumer<byte[], byte[]> BuildOriginConsumer(KafkaRetryConfig config, TopicNames names);
    IConsumer<byte[], byte[]> BuildRetryConsumer(KafkaRetryConfig config, TopicNames names);
}