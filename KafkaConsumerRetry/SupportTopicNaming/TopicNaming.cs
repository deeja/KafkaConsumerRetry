using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.SupportTopicNaming;

public class TopicNaming
    : ITopicNaming {
    public Configuration.TopicNames GetTopicNaming(string topic, KafkaRetryConfig config) {
        var retries = new List<string>();
        for (var i = 0; i < config.RetryAttempts; i++) {
            retries.Add($"{topic}.retry.{i}");
        }

        return new Configuration.TopicNames(topic, retries.ToArray(), topic + ".dlq");
    }
}