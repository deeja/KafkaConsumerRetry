using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.DelayCalculators;

public class BackOffDelayCalculator : IDelayCalculator {
    private readonly RetryServiceConfig _config;

    public BackOffDelayCalculator(RetryServiceConfig config) {
        _config = config;
    }

    public TimeSpan Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex) {
        var originalTime = new DateTimeOffset(consumeResult.Message.Timestamp.UtcDateTime, TimeSpan.Zero);

        var totalMilliseconds = originalTime.ToUnixTimeMilliseconds() +
                                retryIndex * _config.RetryBaseTime.TotalMilliseconds -
                                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return TimeSpan.FromMilliseconds(Math.Max(0, totalMilliseconds));
    }
}