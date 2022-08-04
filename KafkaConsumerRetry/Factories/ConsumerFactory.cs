using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Factories;

public class ConsumerFactory : IConsumerFactory {
    private readonly RetryServiceConfig _config;
    private readonly IPartitionManager _manager;

    public ConsumerFactory(RetryServiceConfig config, IPartitionManager manager) {
        _config = config;
        _manager = manager;
    }

    public IConsumer<byte[], byte[]> BuildOriginConsumer(TopicNaming naming) {
        var consumerConfig = new ConsumerConfig(_config.TopicKafka);
        return BuildConsumer(consumerConfig, naming);
    }

    public IConsumer<byte[], byte[]> BuildRetryConsumer(TopicNaming naming) {
        var consumerConfig = new ConsumerConfig(_config.RetryKafka ?? _config.TopicKafka);
        return BuildConsumer(consumerConfig, naming);
    }

    private IConsumer<byte[], byte[]> BuildConsumer(ConsumerConfig consumerConfig, TopicNaming naming) {
        var consumerBuilder = new ConsumerBuilder<byte[], byte[]>(consumerConfig);
        // TODO: Passing the TopicNaming through here even though it's a rubbish idea. Will figure it out later.

        // (Incremental balancing) Assign/Unassign must not be called in the handler.
        consumerBuilder.SetPartitionsLostHandler((consumer, list) =>
            _manager.HandleLostPartitions(consumer, list));
        consumerBuilder.SetPartitionsAssignedHandler((consumer, list) =>
            _manager.HandleAssignedPartitions(consumer, list, naming));
        consumerBuilder.SetPartitionsRevokedHandler((consumer, list) =>
            _manager.HandleRevokedPartitions(consumer, list));
        return consumerBuilder.Build();
    }
}