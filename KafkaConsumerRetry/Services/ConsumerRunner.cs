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
    private readonly IPartitionProcessorRepository _eventHandler;

    public ConsumerRunner(IConsumerFactory consumerFactory,
        IPartitionProcessorRepository eventHandler) {
        _consumerFactory = consumerFactory;
        _eventHandler = eventHandler;
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
                await _eventHandler.QueueConsumeResultAsync<TResultHandler>(consumeResult);
            }
            catch (TaskCanceledException) {
                // handled in loop condition
            }
        }
    }
}