namespace KafkaConsumerRetry.Services;

public class SemaphoreRateLimiter : IRateLimiter {
    private readonly SemaphoreSlim _semaphore;

    public SemaphoreRateLimiter(int maxCount) {
        _semaphore = new SemaphoreSlim(maxCount, maxCount);
    }

    public void Release() {
        _semaphore.Release();
    }

    public async Task WaitAsync(CancellationToken cancellationToken) {
        await Task.Yield();
        await _semaphore.WaitAsync(cancellationToken);
    }
}