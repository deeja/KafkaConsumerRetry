

namespace KafkaConsumerRetry.Tests.DelayCalculators.BackOffDelayCalculatorTests {
    public class CalculateShould {
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(0, 1, 1)]
        [InlineData(0, 2, 2)]
        [InlineData(-1, 0, 0)]
        [InlineData(-1, 1, 0)]
        [InlineData(-1, 2, 1)]
        [InlineData(1, 0, 1)] // Time in the future. Doesn't really make sense but including it anyway
        [InlineData(1, 1, 2)]
        public void Return_A_Delay(int minutesAfterCurrentTime, int retryCount, int expectedDelay) {
            var retryServiceConfig = new RetryServiceConfig {
                RetryBaseTime = TimeSpan.FromMinutes(1)
            };
            BackOffDelayCalculator calculator = new(retryServiceConfig);

            var consumeResult = new ConsumeResult<byte[], byte[]> {
                Message = new Message<byte[], byte[]> {
                    Timestamp = new Timestamp(DateTimeOffset.Now + TimeSpan.FromMinutes(minutesAfterCurrentTime))
                }
            };
            var timeSpan = calculator.Calculate(consumeResult, retryCount);
            timeSpan.Should().BeCloseTo(TimeSpan.FromMinutes(expectedDelay), TimeSpan.FromSeconds(1));
        }
    }
}