using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;

namespace KafkaConsumerRetry.Services;

public interface IPartitionProcessorRepository {
    /// <summary>
    ///     Queues message into a partition handler
    /// </summary>
    /// <param name="consumeResult"></param>
    /// <typeparam name="TResultHandler"></typeparam>
    Task QueueConsumeResultAsync<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult)
        where TResultHandler : IConsumerResultHandler;

    Task RemoveProcessorAsync(TopicPartition topicPartition, RemovePartitionAction action);
    void AddProcessor(IPartitionProcessor partitionProcessor, TopicPartition topicPartition);
}