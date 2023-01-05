using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public class ConsumerBuilderFactory : IConsumerBuilderFactory {

    public virtual ConsumerBuilder<byte[], byte[]> CreateConsumerBuilder(ConsumerConfig consumerConfig) {
        return new ConsumerBuilder<byte[], byte[]>(consumerConfig);
    }
}