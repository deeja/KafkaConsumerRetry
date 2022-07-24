using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services
{
    /// <summary>
    /// For processing the incoming messages after the have been allocated to run
    /// </summary>
    public interface IConsumerResultHandler
    {
        Task HandleAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    }

    class WriteToLoggerConsumerResultHandler : IConsumerResultHandler {
        private readonly ILogger<WriteToLoggerConsumerResultHandler> _logger;
        public WriteToLoggerConsumerResultHandler(ILogger<WriteToLoggerConsumerResultHandler> logger) {
            _logger = logger;
        }

        public Task HandleAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken) {
            _logger.LogInformation("Message received -Topic: {TopicPartitionOffset}", message.TopicPartitionOffset);
        }
    }
}