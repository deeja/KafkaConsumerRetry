using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public interface IPartitionManager {
    void QueueConsumeResult(ConsumeResult<byte[], byte[]> consumeResult);
    void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list);

    void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartition> list,
        TopicNaming topicNaming);

    void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list);
}