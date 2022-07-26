using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumerRetry.Services {
    public interface ILimiter {
        Task WaitAsync(CancellationToken cancellationToken);
        void Release();
    }
}