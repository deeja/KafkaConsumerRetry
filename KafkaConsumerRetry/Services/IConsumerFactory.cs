using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumerRetry.Services {
    public interface IConsumerFactory {
        Task StartConsumers(string topicName, CancellationToken token);
    }
}