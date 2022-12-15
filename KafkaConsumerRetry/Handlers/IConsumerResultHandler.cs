using Confluent.Kafka;

namespace KafkaConsumerRetry.Handlers;

/// <summary>
///     For processing the incoming messages after the have been allocated to run
/// </summary>
public interface IConsumerResultHandler {
    Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken);
}