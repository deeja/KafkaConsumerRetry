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
            var partitionQueue = _partitionQueues[topicPartition];
            partitionQueue.Enqueue<TResultHandler>(consumeResult);
        }

        // TODO: very rare error here that occurs when a message is enqueued before the partition is added
        try {
            Enqueue();
        }
        catch (KeyNotFoundException keyNotFoundException) {
            var delay = TimeSpan.FromSeconds(1);
            _logger.LogWarning(keyNotFoundException, "Partition key not found: {PartitionKey}. Delaying retry for {DelayTime}", topicPartition, delay);
            await Task.Delay(delay);
            Enqueue();
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