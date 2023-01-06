using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry.Tests.RateLimiterTests;

public class WaitAsyncShould {
    [Fact]
    public async Task Allow_Entry_To_First_Only() {
        var rateLimiter = new SemaphoreRateLimiter(1);
        await rateLimiter.WaitAsync(CancellationToken.None);
        var wait = rateLimiter.WaitAsync(CancellationToken.None);
        await Task.Delay(200);
        wait.IsCompleted.Should().BeFalse("Semaphore should be blocking task");
    }

    [Fact]
    public async Task Allow_Entry_To_Second_After_Release() {
        var rateLimiter = new SemaphoreRateLimiter(1);
        await rateLimiter.WaitAsync(CancellationToken.None);
        var waiting = rateLimiter.WaitAsync(CancellationToken.None);
        await Task.Delay(200);
        waiting.IsCompleted.Should().BeFalse("Semaphore should be blocking task");
        rateLimiter.Release();
        await waiting;
        waiting.IsCompleted.Should().BeTrue();
    }
}