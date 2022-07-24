using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public interface IConsumerFactory {
        Task StartConsumers(CancellationToken token, TopicNaming topicNaming);
    }
}