using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public interface IPartitionManager {
    void QueueConsumeResult<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult) where TResultHandler: IConsumerResultHandler;
    void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartitionOffset> list);

    void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartition> list,
        TopicNaming topicNaming, ProducerConfig producerConfig);

    void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartitionOffset> list);
}