using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Handlers;

namespace KafkaConsumerRetry.Services;

public interface IPartitionMessageManager {
    void QueueConsumeResult<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult) where TResultHandler : IConsumerResultHandler;

    void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list);

    void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartition> list,
        TopicNames topicNames, ProducerConfig producerConfig);

    void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list);
}