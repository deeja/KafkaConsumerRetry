namespace KafkaConsumerRetry.Services;

public interface IRateLimiter {
    Task WaitAsync(CancellationToken cancellationToken);
    void Release();
}