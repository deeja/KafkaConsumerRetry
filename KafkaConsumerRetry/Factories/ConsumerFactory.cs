using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Factories;

public class ConsumerFactory : IConsumerFactory {
    private readonly IPartitionManager _manager;

    public ConsumerFactory(IPartitionManager manager) {
        _manager = manager;
    }

    public IConsumer<byte[], byte[]> BuildOriginConsumer(KafkaRetryConfig config, TopicNaming naming) {
        var consumerConfig = new ConsumerConfig(config.TopicKafka);
        var producerConfig = new ProducerConfig(config.RetryKafka ?? config.TopicKafka);
        return BuildConsumer(consumerConfig, naming, producerConfig);
    }

    public IConsumer<byte[], byte[]> BuildRetryConsumer(KafkaRetryConfig config, TopicNaming naming) {
        var consumerConfig = new ConsumerConfig(config.RetryKafka ?? config.TopicKafka);
        var producerConfig = new ProducerConfig(config.RetryKafka ?? config.TopicKafka);
        return BuildConsumer(consumerConfig, naming, producerConfig);
    }

    private IConsumer<byte[], byte[]> BuildConsumer(ConsumerConfig consumerConfig, TopicNaming naming,
        ProducerConfig producerConfig) {
        var consumerBuilder = new ConsumerBuilder<byte[], byte[]>(consumerConfig);
        // TODO: Passing the TopicNaming through here even though it's a rubbish idea. Will figure it out later.

        // (Incremental balancing) Assign/Unassign must not be called in the handler.
        consumerBuilder.SetPartitionsLostHandler((consumer, list) =>
            _manager.HandleLostPartitions(consumer, consumerConfig, list));
        consumerBuilder.SetPartitionsAssignedHandler((consumer, list) =>
            _manager.HandleAssignedPartitions(consumer,  consumerConfig, list, naming, producerConfig));
        consumerBuilder.SetPartitionsRevokedHandler((consumer, list) =>
            _manager.HandleRevokedPartitions(consumer,  consumerConfig, list));
        return consumerBuilder.Build();
    }
}