using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services
{
    public interface ITopicManager
    {
        Task SetupTopics(TopicNaming topicsNaming);
    }
}