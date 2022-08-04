using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services;

public class RateLimiter : IRateLimiter {
    private readonly SemaphoreSlim _semaphore;

    public RateLimiter(RetryServiceConfig config) {
        _semaphore = new SemaphoreSlim(config.MaxConcurrent);
    }

    public void Release() {
        _semaphore.Release();
    }

    public async Task WaitAsync(CancellationToken cancellationToken) {
        await _semaphore.WaitAsync(cancellationToken);
    }
}