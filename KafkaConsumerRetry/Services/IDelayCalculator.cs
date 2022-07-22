using System;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    public interface IDelayCalculator
    {
        TimeSpan Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex);
    }
}