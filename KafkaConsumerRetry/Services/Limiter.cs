using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public class Limiter : ILimiter {
        private readonly SemaphoreSlim _semaphore;

        public Limiter(RetryServiceConfig config) {
            _semaphore = new SemaphoreSlim(config.MaxConcurrent);
        }

        public void Release() {
            _semaphore.Release();
        }

        public async Task WaitAsync(CancellationToken cancellationToken) {
            await _semaphore.WaitAsync(cancellationToken);
        }
    }
}