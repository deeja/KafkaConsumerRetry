using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Logging;

namespace TestConsole {
    internal class WriteToLoggerConsumerResultHandler : IConsumerResultHandler {
        private readonly ILogger<WriteToLoggerConsumerResultHandler> _logger;

        public WriteToLoggerConsumerResultHandler(ILogger<WriteToLoggerConsumerResultHandler> logger) {
            _logger = logger;
        }

        public Task HandleAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken) {
            _logger.LogInformation("Message received - Topic: {TopicPartitionOffset}", message.TopicPartitionOffset);
            throw new NotImplementedException("Kaboom!");
        }
    }
}