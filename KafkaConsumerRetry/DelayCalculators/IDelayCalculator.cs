using Confluent.Kafka;

namespace KafkaConsumerRetry.DelayCalculators;

public interface IDelayCalculator {
    TimeSpan Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex);
}