using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Factories;

public class ConsumerFactory : IConsumerFactory {
    private readonly IPartitionMessageManager _messageManager;

    public ConsumerFactory(IPartitionMessageManager messageManager) {
        _messageManager = messageManager;
    }

    public virtual IConsumer<byte[], byte[]> BuildOriginConsumer(KafkaRetryConfig config, TopicNames names) {
        var cluster = config.OriginCluster;
        SetClusterDefaults(cluster);
        var consumerConfig = new ConsumerConfig(cluster);
        var producerConfig = new ProducerConfig(config.RetryCluster ?? cluster);
        return BuildConsumer(consumerConfig, names, producerConfig);
    }

    public virtual IConsumer<byte[], byte[]> BuildRetryConsumer(KafkaRetryConfig config, TopicNames names) {
        var cluster = config.RetryCluster ?? config.OriginCluster;
        SetClusterDefaults(cluster);
        var consumerConfig = new ConsumerConfig(cluster);
        var producerConfig = new ProducerConfig(cluster);
        return BuildConsumer(consumerConfig, names, producerConfig);
    }

    protected virtual void SetClusterDefaults(IDictionary<string, string> clusterSettings) {
        clusterSettings["auto.offset.reset"] = "earliest"; // Get the first available messages when setting up consumer group
        clusterSettings["enable.auto.offset.store"] = "false"; //Don't auto save the offset; this is done inside the error handling
        clusterSettings["enable.auto.commit"] = "true"; // Allow auto commit
    }


    protected virtual IConsumer<byte[], byte[]> BuildConsumer(ConsumerConfig consumerConfig, TopicNames names,
        ProducerConfig producerConfig) {
        var consumerBuilder = new ConsumerBuilder<byte[], byte[]>(consumerConfig);
        consumerBuilder.SetPartitionsAssignedHandler((consumer, list) =>
            _messageManager.HandleAssignedPartitions(consumer, consumerConfig, list, names, producerConfig));
        consumerBuilder.SetPartitionsLostHandler((consumer, list) => _messageManager.HandleLostPartitions(consumer, list));
        consumerBuilder.SetPartitionsRevokedHandler((consumer, list) => _messageManager.HandleRevokedPartitions(consumer, list));

        return consumerBuilder.Build();
    }
}