using Confluent.Kafka;

namespace KafkaConsumerRetry.Handlers;

public abstract class TypedHandler<THeader, TValue> : IConsumerResultHandler {
    private readonly IDeserializer<THeader> _headerDeserializer;
    private readonly IDeserializer<TValue> _valueDeserializer;

    protected TypedHandler(IDeserializer<THeader> headerDeserializer, IDeserializer<TValue> valueDeserializer) {
        _headerDeserializer = headerDeserializer;
        _valueDeserializer = valueDeserializer;
    }

    public async Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        var typedKey = Deserialize(consumeResult, MessageComponentType.Key, _headerDeserializer);
        var typedValue = Deserialize(consumeResult, MessageComponentType.Value, _valueDeserializer);
        var message = consumeResult.Message;

        var result = new ConsumeResult<THeader, TValue> {
            Message = new Message<THeader, TValue> {
                Key = await typedKey,
                Value = await typedValue,
                Timestamp = message.Timestamp,
                Headers = message.Headers
            },
            TopicPartitionOffset = consumeResult.TopicPartitionOffset,
            IsPartitionEOF = consumeResult.IsPartitionEOF
        };
        await HandleAsync(result, cancellationToken);
    }

    protected abstract Task HandleAsync(ConsumeResult<THeader, TValue> consumerResult, CancellationToken cancellationToken);

    private Task<T> Deserialize<T>(ConsumeResult<byte[], byte[]> consumeResult, MessageComponentType component, IDeserializer<T> deserializer) {
        var keyContent = new ReadOnlySpan<byte>(consumeResult.Message.Key);
        var keyContext = new SerializationContext(component, consumeResult.Topic, consumeResult.Message.Headers);
        return Task.FromResult(deserializer.Deserialize(keyContent, false, keyContext));
    }
}