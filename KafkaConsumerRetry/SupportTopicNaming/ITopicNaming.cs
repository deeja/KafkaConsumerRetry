namespace KafkaConsumerRetry.SupportTopicNaming;

public interface ITopicNaming {
    Configuration.TopicNaming GetTopicNaming(string topic);
}