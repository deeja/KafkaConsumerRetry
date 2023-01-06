using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public interface IConsumerBuilderFactory {
    IConsumerBuilder CreateConsumerBuilder(ConsumerConfig consumerConfig);
}