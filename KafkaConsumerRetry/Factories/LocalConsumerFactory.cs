using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Factories;

public class LocalConsumerFactory : ILocalConsumerFactory {
    private readonly IConsumerBuilderFactory _consumerBuilderFactory;
    private readonly IPartitionMessageManager _messageManager;

    public LocalConsumerFactory(IPartitionMessageManager messageManager, IConsumerBuilderFactory consumerBuilderFactory) {
        _messageManager = messageManager;
        _consumerBuilderFactory = consumerBuilderFactory;
    }

    public virtual IConsumer<byte[], byte[]> BuildConsumer(ConsumerConfig consumerConfig,
        ProducerConfig producerConfig, TopicNames names) {
        var consumerBuilder = _consumerBuilderFactory.CreateConsumerBuilder(consumerConfig);
        SetPartitionEventHandlers(consumerBuilder, consumerConfig, producerConfig, names);
        return consumerBuilder.Build();
    }

    protected virtual void SetPartitionEventHandlers(IConsumerBuilder consumerBuilder, ConsumerConfig consumerConfig,
        ProducerConfig producerConfig,
        TopicNames names) {
        consumerBuilder.SetPartitionsAssignedHandler((consumer, list) =>
            _messageManager.HandleAssignedPartitions(consumer, consumerConfig, list, names, producerConfig));
        consumerBuilder.SetPartitionsLostHandler((consumer, list) => _messageManager.HandleLostPartitions(consumer, list));
        consumerBuilder.SetPartitionsRevokedHandler((consumer, list) => _messageManager.HandleRevokedPartitions(consumer, list));
    }

}