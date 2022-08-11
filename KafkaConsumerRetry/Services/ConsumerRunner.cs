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

    public virtual async Task RunConsumersAsync<TResultHandler>(KafkaRetryConfig kafkaRetryConfig,
        TopicNaming topicNaming, CancellationToken token) where TResultHandler : IConsumerResultHandler {
        var originConsumer = _consumerFactory.BuildOriginConsumer(kafkaRetryConfig, topicNaming);
        var retryConsumer = _consumerFactory.BuildRetryConsumer(kafkaRetryConfig, topicNaming);

        originConsumer.Subscribe(topicNaming.Origin);
        retryConsumer.Subscribe(topicNaming.Retries);

        var originConsumerTask = Task.Run(() => ConsumeAsync<TResultHandler>(originConsumer, token),token);
        var retryConsumerTask = Task.Run(() => ConsumeAsync<TResultHandler>(retryConsumer, token),token);
        await Task.WhenAll(originConsumerTask, retryConsumerTask);
    }

    private void ConsumeAsync<TResultHandler>(IConsumer<byte[], byte[]> consumer,
        CancellationToken cancellationToken) where TResultHandler : IConsumerResultHandler {
        while (!cancellationToken.IsCancellationRequested) {
            var consumeResult = consumer.Consume(cancellationToken);

            _manager.QueueConsumeResult<TResultHandler>(consumeResult);
        }
    }
}