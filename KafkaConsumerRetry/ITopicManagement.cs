using System.Collections.Generic;
using System.Threading.Tasks;

namespace KafkaConsumerRetry
{
    public interface ITopicManagement
    {
        Task EnsureTopicSettings(TopicNaming topicsNaming);
    }
}