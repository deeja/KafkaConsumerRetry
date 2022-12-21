using Confluent.Kafka;

namespace KafkaConsumerRetry.DelayCalculators;

public class MultiplyingBackOffCalculator : IDelayCalculator {
    private readonly TimeSpan _baseTime;

    public MultiplyingBackOffCalculator(TimeSpan baseTime) {
        _baseTime = baseTime;
    }

    public DateTimeOffset Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex) {
        var unixTimestampMs = consumeResult.Message.Timestamp.UnixTimestampMs;
        var timeToAdd = retryIndex * _baseTime.TotalMilliseconds;
        var retryTime = UnixTimestampMsToDateTime(unixTimestampMs + timeToAdd);
        return new DateTimeOffset(retryTime, TimeSpan.Zero);
    }

    private static DateTime UnixTimestampMsToDateTime(double unixMillisecondsTimestamp) {
        return Timestamp.UnixTimeEpoch + TimeSpan.FromMilliseconds(unixMillisecondsTimestamp);
    }
}