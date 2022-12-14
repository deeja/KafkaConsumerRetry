using Confluent.Kafka;
using KafkaConsumerRetry.Services;

public class MyCustomHandler : IConsumerResultHandler {
    public Task HandleAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken) {
        // The messages can be de-serialised or otherwise used here 
        return Task.CompletedTask;
    }
}