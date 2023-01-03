using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Handlers;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Starts and subscribes to messages
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ConsumerRunner : IConsumerRunner {
    private readonly IConsumerFactory _consumerFactory;
    private readonly IPartitionMessageManager _messageManager;

    public ConsumerRunner(IConsumerFactory consumerFactory,
        IPartitionMessageManager messageManager) {
        _consumerFactory = consumerFactory;
        _messageManager = messageManager;
    }

    public virtual async Task RunConsumersAsync<TResultHandler>(KafkaRetryConfig kafkaRetryConfig,
        TopicNames topicNames, CancellationToken token) where TResultHandler : IConsumerResultHandler {
        var originConsumer = _consumerFactory.BuildOriginConsumer(kafkaRetryConfig, topicNames);
        var retryConsumer = _consumerFactory.BuildRetryConsumer(kafkaRetryConfig, topicNames);

        originConsumer.Subscribe(topicNames.Origin);
        retryConsumer.Subscribe(topicNames.Retries);

        var originConsumerTask = ConsumeAsync<TResultHandler>(originConsumer, token);
        var retryConsumerTask = ConsumeAsync<TResultHandler>(retryConsumer, token);
        await Task.WhenAll(originConsumerTask, retryConsumerTask);
    }

    private async Task ConsumeAsync<TResultHandler>(IConsumer<byte[], byte[]> consumer,
        CancellationToken cancellationToken) where TResultHandler : IConsumerResultHandler {
        await Task.Yield();
        while (!cancellationToken.IsCancellationRequested) {
            try {
                var consumeResult = consumer.Consume(cancellationToken);
                await _messageManager.QueueConsumeResultAsync<TResultHandler>(consumeResult);
            }
            catch (TaskCanceledException) {
                // handled in loop condition
            }
        }
    }
}