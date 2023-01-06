using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public class TopicNaming
    : ITopicNaming {
    public TopicNames GetTopicNaming(string topic, KafkaRetryConfig config) {
        var retries = new List<string>();
        for (var i = 0; i < config.RetryAttempts; i++) {
            retries.Add($"{topic}.retry.{i}");
        }

        return new TopicNames(topic, retries.ToArray(), topic + ".dlq");
    }
}