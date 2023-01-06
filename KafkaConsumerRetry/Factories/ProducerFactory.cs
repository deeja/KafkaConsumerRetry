using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public class ProducerFactory : IProducerFactory {
    private readonly IProducerBuilderFactory _producerBuilderFactory;
    public ProducerFactory(IProducerBuilderFactory producerBuilderFactory) {
        _producerBuilderFactory = producerBuilderFactory;
    }

    public IProducer<byte[], byte[]> BuildRetryProducer(ProducerConfig config) {
        return _producerBuilderFactory.CreateProducerBuilder(config).Build();
    }
}