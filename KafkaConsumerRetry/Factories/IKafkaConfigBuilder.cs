using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public interface IKafkaConfigBuilder {
    ConsumerConfig BuildConsumerConfig(IDictionary<string, string> keyValues);
    ProducerConfig BuildProducerConfig(IDictionary<string, string> keyValues);
}