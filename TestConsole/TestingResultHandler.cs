using System.Diagnostics;
using System.Text;
using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.Logging;

namespace TestConsole;

internal class TestingResultHandler : IConsumerResultHandler {


    private static int _count;
    
    private static Stopwatch _timer = Stopwatch.StartNew();
    private readonly ILogger<TestingResultHandler> _logger;

    public TestingResultHandler(ILogger<TestingResultHandler> logger) {
        _logger = logger;
    }

    public async Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        var messageString = Encoding.UTF8.GetString(consumeResult.Message.Value);

        Interlocked.Increment(ref _count);

        _logger.LogInformation("{Timer} #{Count}  - Message: {MessageString} - Topic: {TopicPartitionOffset}", _timer.Elapsed, _count, messageString,
            consumeResult.TopicPartitionOffset);
        var delay = TimeSpan.FromSeconds(0);
        
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
                _logger.LogInformation("## Throwing exception in {TimeSpan} ##", delay);
                await Task.Delay(delay, cancellationToken);
                throw new GoBoomException();
            default:
                _logger.LogInformation("## Delaying return by {TimeSpan} ##", delay);
                await Task.Delay(delay, cancellationToken);
                break;
        }
    }
}