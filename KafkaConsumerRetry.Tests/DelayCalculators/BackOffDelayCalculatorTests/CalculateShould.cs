using KafkaConsumerRetry.DelayCalculators;

namespace KafkaConsumerRetry.Tests.DelayCalculators.BackOffDelayCalculatorTests;

public class CalculateShould {
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void Return_A_Delay(int retryCount, int expectedDelay) {
        MultiplyingBackOffCalculator calculator = new(TimeSpan.FromMinutes(1));

        var referenceTime = new DateTimeOffset(2022, 10, 09, 12, 10, 0, TimeSpan.FromHours(10.5));

        var consumeResult = new ConsumeResult<byte[], byte[]> {
            Message = new Message<byte[], byte[]> {
                Timestamp = new Timestamp(referenceTime)
            }
        };
        var actual = calculator.Calculate(consumeResult, retryCount);

        var expected = (referenceTime + TimeSpan.FromMinutes(expectedDelay)).UtcDateTime;

        actual.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
    }
}