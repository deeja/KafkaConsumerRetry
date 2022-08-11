using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.SupportTopicNaming;

public interface ITopicNaming {
    Configuration.TopicNaming GetTopicNaming(string topic, KafkaRetryConfig config);
}