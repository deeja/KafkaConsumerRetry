using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.SupportTopicNaming;

public interface ITopicNaming {
    TopicNames GetTopicNaming(string topic, KafkaRetryConfig config);
}