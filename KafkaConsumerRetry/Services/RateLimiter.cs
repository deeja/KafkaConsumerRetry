namespace KafkaConsumerRetry.Services;

internal class RateLimiter : IRateLimiter {
    private readonly SemaphoreSlim _semaphore;

    internal RateLimiter(int maxCount) {
        _semaphore = new SemaphoreSlim(maxCount);
    }

    public void Release() {
        _semaphore.Release();
    }

    public async Task WaitAsync(CancellationToken cancellationToken) {
        await _semaphore.WaitAsync(cancellationToken);
    }
}