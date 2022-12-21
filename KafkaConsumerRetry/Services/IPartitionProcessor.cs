using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;

namespace KafkaConsumerRetry.Services;

public interface IPartitionProcessor {
    void Dispose();

    /// <summary>
    /// </summary>
    /// <remarks>
    ///     This code runs per topic partition, so access is assumed to be sequential (unlikely to have multiple threads)
    /// </remarks>
    /// <param name="consumeResult"></param>
    void Enqueue<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult) where TResultHandler : IConsumerResultHandler;

    void Cancel();
    Task RevokeAsync();
    void Start();
}