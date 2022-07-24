using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public interface IConsumerFactory {
        Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token);
    }
}