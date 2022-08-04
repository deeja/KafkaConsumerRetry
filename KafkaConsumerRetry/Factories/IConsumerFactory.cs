using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public interface IConsumerFactory {
    IConsumer<byte[], byte[]> BuildOriginConsumer(TopicNaming naming);
    IConsumer<byte[], byte[]> BuildRetryConsumer(TopicNaming naming);
}