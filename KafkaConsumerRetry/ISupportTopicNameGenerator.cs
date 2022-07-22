namespace KafkaConsumerRetry
{
    public interface ISupportTopicNameGenerator
    {
        TopicNaming GetTopicNaming(string topic);
    }
}