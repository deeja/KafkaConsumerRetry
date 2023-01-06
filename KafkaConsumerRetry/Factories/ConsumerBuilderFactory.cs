using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public class ConsumerBuilderFactory : IConsumerBuilderFactory {

    public virtual IConsumerBuilder CreateConsumerBuilder(ConsumerConfig consumerConfig) {
        return new LocalConsumerBuilder(new ConsumerBuilder<byte[], byte[]>(consumerConfig));
    }
}