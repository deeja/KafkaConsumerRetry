using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Services;

/// <summary>
///     Starts and subscribes to messages
/// </summary>
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

        var originConsumerTask = Task.Run(() => ConsumeAsync<TResultHandler>(originConsumer, token), token);
        var retryConsumerTask = Task.Run(() => ConsumeAsync<TResultHandler>(retryConsumer, token), token);
        await Task.WhenAll(originConsumerTask, retryConsumerTask);
    }

    private void ConsumeAsync<TResultHandler>(IConsumer<byte[], byte[]> consumer,
        CancellationToken cancellationToken) where TResultHandler : IConsumerResultHandler {
        while (!cancellationToken.IsCancellationRequested) {
            var consumeResult = consumer.Consume(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) {
                break;
            }

            _messageManager.QueueConsumeResult<TResultHandler>(consumeResult);
        }
    }
}