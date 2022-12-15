using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;

public class ExampleTypedHandler : TypedHandler<string, MyEvent> {

    public ExampleTypedHandler(IDeserializer<string> headerDeserializer, IDeserializer<MyEvent> valueDeserializer) : base(headerDeserializer,
        valueDeserializer) { }

    protected override Task HandleAsync(ConsumeResult<string, MyEvent> consumerResult, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}