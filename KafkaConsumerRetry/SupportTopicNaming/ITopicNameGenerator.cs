namespace KafkaConsumerRetry.SupportTopicNaming;

public interface ITopicNameGenerator {
    Configuration.TopicNaming GetTopicNaming(string topic);
}