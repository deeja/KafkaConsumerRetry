using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public interface IConsumerRunner {
    Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token);
}