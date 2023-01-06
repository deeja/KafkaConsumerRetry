using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public interface ILocalConsumerFactory {
    IConsumer<byte[], byte[]> BuildConsumer(ConsumerConfig consumerConfig,
        ProducerConfig producerConfig, TopicNames names);
}