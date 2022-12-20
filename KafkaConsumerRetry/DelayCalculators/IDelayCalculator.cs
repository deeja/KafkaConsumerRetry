using Confluent.Kafka;

namespace KafkaConsumerRetry.DelayCalculators;

public interface IDelayCalculator {
    DateTimeOffset Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex);
}