using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services
{
    public interface ISupportTopicNameGenerator
    {
        TopicNaming GetTopicNaming(string topic);
    }
}