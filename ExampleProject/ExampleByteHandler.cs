using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;

namespace ExampleProject;

public class ExampleByteHandler : IConsumerResultHandler {
    public Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        // handle the bytes as needed
        return Task.CompletedTask;
    }
}