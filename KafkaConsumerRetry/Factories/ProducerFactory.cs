using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public class ProducerFactory : IProducerFactory {
    
    public IProducer<byte[], byte[]> BuildRetryProducer(ProducerConfig config) {
        return new ProducerBuilder<byte[], byte[]>(config).Build();
    }
}