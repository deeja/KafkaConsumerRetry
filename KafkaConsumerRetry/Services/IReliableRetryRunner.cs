using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public interface IReliableRetryRunner {
        Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token);
    }
}