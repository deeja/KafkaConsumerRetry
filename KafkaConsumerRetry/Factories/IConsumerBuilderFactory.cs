using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public interface IConsumerBuilderFactory {
    ConsumerBuilder<byte[], byte[]> CreateConsumerBuilder(ConsumerConfig consumerConfig);
}