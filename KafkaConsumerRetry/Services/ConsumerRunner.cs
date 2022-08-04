using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Starts and subscribes to messages
/// </summary>
public class ConsumerRunner : IConsumerRunner {
    private readonly IConsumerFactory _consumerFactory;
    private readonly IPartitionManager _manager;

    public ConsumerRunner(IConsumerFactory consumerFactory,
        IPartitionManager manager) {
        _consumerFactory = consumerFactory;
        _manager = manager;
    }

    public virtual async Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token) {
        var originConsumer = _consumerFactory.BuildOriginConsumer(topicNaming);
        var retryConsumer = _consumerFactory.BuildRetryConsumer(topicNaming);

        originConsumer.Subscribe(topicNaming.Origin);

        retryConsumer.Subscribe(topicNaming.Retries);

        await Task.WhenAll(ConsumeAsync(originConsumer, token),
            ConsumeAsync(retryConsumer, token));
    }

    private async Task ConsumeAsync(IConsumer<byte[], byte[]> consumer,
        CancellationToken cancellationToken) {
        await Task.Yield();
        while (!cancellationToken.IsCancellationRequested) {
            var consumeResult = consumer.Consume(cancellationToken);

            _manager.QueueConsumeResult(consumeResult);
        }
    }
}