using Confluent.Kafka;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public class PartitionProcessorRepository : IPartitionProcessorRepository {
    private readonly ILogger<PartitionProcessorRepository> _logger;

    private readonly Dictionary<TopicPartition, IPartitionProcessor> _partitionQueues = new();

    public PartitionProcessorRepository(ILogger<PartitionProcessorRepository> logger) {
        _logger = logger;
    }

    /// <summary>
    ///     Queues message into a partition handler
    /// </summary>
    /// <param name="consumeResult"></param>
    /// <typeparam name="TResultHandler"></typeparam>
    public virtual async Task QueueConsumeResultAsync<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult)
        where TResultHandler : IConsumerResultHandler {
        var topicPartition = consumeResult.TopicPartition;

        void Enqueue() {
            var partitionProcessor = _partitionQueues[topicPartition];
            partitionProcessor.Enqueue<TResultHandler>(consumeResult);
        }

        var count = 0;

        while (true) {
            // occasional error here that occurs when a message is enqueued before the partition processor is added
            try {
                Enqueue();
                break;
            }
            catch (KeyNotFoundException keyNotFoundException) {
                var delay = TimeSpan.FromMilliseconds(100);
                _logger.LogWarning(keyNotFoundException, "Partition key not found: {PartitionKey}. Delaying retry for {DelayTime}", topicPartition, delay);

                // if we have retried enough
                if (count > 10) {
                    throw;
                }

                await Task.Delay(delay);
            }

            count++;
        }
    }



    public virtual async Task RemoveProcessorAsync(TopicPartition topicPartition, RemovePartitionAction action) {
        var partitionProcessor = _partitionQueues[topicPartition];
        _partitionQueues.Remove(topicPartition);
        switch (action) {
            case RemovePartitionAction.Cancel:
                partitionProcessor.Cancel();
                break;
            case RemovePartitionAction.Revoke:
                await partitionProcessor.RevokeAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }



    public virtual void AddProcessor(IPartitionProcessor partitionProcessor, TopicPartition topicPartition) {
        _partitionQueues.Add(topicPartition, partitionProcessor);
        partitionProcessor.Start();
    }
}