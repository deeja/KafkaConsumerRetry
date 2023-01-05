using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Factories;

public class ConsumerFactory : IConsumerFactory {
    private readonly ILocalConsumerBuilder _builder;
    private readonly IKafkaConfigBuilder _kafkaConfigBuilder;

    public ConsumerFactory(ILocalConsumerBuilder builder, IKafkaConfigBuilder kafkaConfigBuilder) {
        _builder = builder;
        _kafkaConfigBuilder = kafkaConfigBuilder;
    }

    public virtual IConsumer<byte[], byte[]> BuildOriginConsumer(KafkaRetryConfig config, TopicNames names) {
        var consumerCluster = config.OriginCluster;
        var producerCluster = config.RetryCluster ?? consumerCluster;
        var consumerConfig = _kafkaConfigBuilder.BuildConsumerConfig(consumerCluster);
        var producerConfig = _kafkaConfigBuilder.BuildProducerConfig(producerCluster);
        return _builder.BuildConsumer(consumerConfig, producerConfig, names);
    }

    public virtual IConsumer<byte[], byte[]> BuildRetryConsumer(KafkaRetryConfig config, TopicNames names) {
        var cluster = config.RetryCluster ?? config.OriginCluster;
        var consumerConfig = _kafkaConfigBuilder.BuildConsumerConfig(cluster);
        var producerConfig = _kafkaConfigBuilder.BuildProducerConfig(cluster);
        return _builder.BuildConsumer(consumerConfig, producerConfig, names);
    }
}