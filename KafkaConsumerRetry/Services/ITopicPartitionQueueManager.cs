using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    public interface ITopicPartitionQueueManager {
        void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult, IConsumer<byte[], byte[]> consumer,
            string retryGroupId, string nextTopic, int retryIndex);
    }
}