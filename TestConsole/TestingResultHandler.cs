using System.Text;
using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.Logging;

namespace TestConsole;

internal class TestingResultHandler : IConsumerResultHandler {


    private static int _count;
    private readonly ILogger<TestingResultHandler> _logger;

    public TestingResultHandler(ILogger<TestingResultHandler> logger) {
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        var messageString = Encoding.UTF8.GetString(consumeResult.Message.Value);

        Interlocked.Increment(ref _count);

        _logger.LogInformation(" #{Count} - Message: {MessageString} - Topic: {TopicPartitionOffset}", _count, messageString,
            consumeResult.TopicPartitionOffset);

        switch (messageString) {
            case "DIE":
                _logger.LogInformation("## Failing FAST ##");
                Environment.FailFast(string.Empty);
                break;
            case "SLOW":
                _logger.LogInformation("## Failing SLOW ##");
                Environment.Exit(1);
                break;
            case "THROW":
                var fromSeconds = TimeSpan.FromSeconds(5);
                _logger.LogInformation("## Throwing exception in {TimeSpan} ##", fromSeconds);
                await Task.Delay(fromSeconds, cancellationToken);
                throw new GoBoomException();
            default:
                _logger.LogInformation("## Wait and continue ##");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                break;
        }
    }
}