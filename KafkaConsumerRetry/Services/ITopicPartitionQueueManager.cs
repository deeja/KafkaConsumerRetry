using System.Collections.Generic;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    public interface ITopicPartitionQueueManager {
        void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult);
        void HandleLostPartitions(IConsumer<byte[],byte[]> consumer, List<TopicPartitionOffset> list);
        void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartition> list);
        void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list);
    }
}