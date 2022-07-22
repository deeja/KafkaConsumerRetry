using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services
{
    public interface ITopicManagement
    {
        Task EnsureTopicSettings(TopicNaming topicsNaming);
    }
}