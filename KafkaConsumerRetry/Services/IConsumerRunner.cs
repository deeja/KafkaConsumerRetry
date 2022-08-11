using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public interface IConsumerRunner {
    Task RunConsumersAsync<TResultHandler>(KafkaRetryConfig kafkaRetryConfig, TopicNaming topicNaming,
        CancellationToken token) where TResultHandler : IConsumerResultHandler;
}