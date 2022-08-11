using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.DelayCalculators;

public class IncrementalBackOffDelayCalculator : IDelayCalculator {
    
    readonly TimeSpan _baseTime = TimeSpan.FromSeconds(5);
    
    public TimeSpan Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex) {
        var originalTime = new DateTimeOffset(consumeResult.Message.Timestamp.UtcDateTime, TimeSpan.Zero);

        var totalMilliseconds = originalTime.ToUnixTimeMilliseconds() +
                                retryIndex * _baseTime.TotalMilliseconds -
                                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return TimeSpan.FromMilliseconds(Math.Max(0, totalMilliseconds));
    }
}