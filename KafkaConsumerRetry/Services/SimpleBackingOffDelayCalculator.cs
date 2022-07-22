using System;
using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace KafkaConsumerRetry {
    public class SimpleBackingOffDelayCalculator: IDelayCalculator {
        private readonly RetryServiceConfig _config;
        public SimpleBackingOffDelayCalculator(RetryServiceConfig config) {
            _config = config;
        }

        public TimeSpan Calculate(ConsumeResult<byte[], byte[]> consumeResult, int retryIndex) {
            return retryIndex == 0 ? TimeSpan.Zero : TimeSpan.FromSeconds(_config.RetryBaseTime.TotalSeconds * retryIndex);
        }
    }
}