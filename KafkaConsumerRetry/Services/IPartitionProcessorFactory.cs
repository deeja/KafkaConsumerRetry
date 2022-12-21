using Confluent.Kafka;

namespace KafkaConsumerRetry.Services;

public interface IPartitionProcessorFactory {
    IPartitionProcessor Create(IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition, int currentIndex, string nextTopic, string retryGroup,
        IProducer<byte[], byte[]> retryProducer);
}