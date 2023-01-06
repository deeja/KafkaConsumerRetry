using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public interface ITopicNaming {
    TopicNames GetTopicNaming(string topic, KafkaRetryConfig config);
}