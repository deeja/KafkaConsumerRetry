using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public abstract class PartitionMessageManagerBase : IPartitionMessageManager {
    private readonly ILogger _logger;

    private readonly IPartitionProcessorFactory _partitionProcessorFactory;
    private readonly Dictionary<TopicPartition, IPartitionProcessor> _partitionQueues = new();
    private readonly IServiceProvider _serviceProvider;

    protected PartitionMessageManagerBase(IPartitionProcessorFactory partitionProcessorFactory, ILogger logger, IServiceProvider serviceProvider) {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _partitionProcessorFactory = partitionProcessorFactory;
    }

    public virtual async Task QueueConsumeResult<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult)
        where TResultHandler : IConsumerResultHandler {
        var topicPartition = consumeResult.TopicPartition;

        void Enqueue() {
            var partitionQueue = _partitionQueues[topicPartition];
            partitionQueue.Enqueue<TResultHandler>(consumeResult);
        }

        // very rare error here that occurs when a message is enqueued before the partition is added.
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

    public virtual void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        _logger.LogInformation("LOST PARTITIONS {TopicPartitions}", list);
        foreach (var topicPartitionOffset in list) {
            _partitionQueues[topicPartitionOffset.TopicPartition].Cancel();
            _partitionQueues.Remove(topicPartitionOffset.TopicPartition);
        }
    }

    public virtual void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartition> list,
        TopicNames topicNames, ProducerConfig producerConfig) {
        _logger.LogInformation("BEGIN ASSIGNED PARTITIONS {TopicPartitions}", list);

        // TODO: Remove this construction of the Retry Producer so it's a singleton somewhere. 
        // Leaving here so it's not per PartitionProcessor
        var retryProducer = _serviceProvider.GetRequiredService<IProducerFactory>().BuildRetryProducer(producerConfig);
        foreach (var topicPartition in list) {
            var currentIndexAndNextTopic = GetCurrentIndexAndNextTopic(topicPartition.Topic, topicNames);
            var partitionProcessor = _partitionProcessorFactory.Create(consumer, topicPartition, currentIndexAndNextTopic.CurrentIndex,
                currentIndexAndNextTopic.NextTopic, consumerConfig.GroupId, retryProducer);
            partitionProcessor.Start();
            _partitionQueues.Add(topicPartition, partitionProcessor);
        }

        _logger.LogInformation("END ASSIGNED PARTITIONS {TopicPartitions}", list);
    }

    public virtual void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        _logger.LogInformation("BEGIN REVOKED PARTITIONS {TopicPartitions}", list);
        List<Task> revokeWaiters = new();
        foreach (var topicPartitionOffset in list) {
            var topicPartition = topicPartitionOffset.TopicPartition;
            var partitionQueue = _partitionQueues[topicPartition];
            _partitionQueues.Remove(topicPartition);
            revokeWaiters.Add(partitionQueue.RevokeAsync());
        }

        _ = Task.WhenAll(revokeWaiters.ToArray()).ContinueWith(task => _logger.LogInformation("END REVOKED PARTITIONS {TopicPartitions}", list));
    }

    /// <summary>
    ///     Gets the current index of the topic, and also the next topic to push to
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="topicNames"></param>
    /// <returns></returns>
    protected virtual (int CurrentIndex, string NextTopic)
        GetCurrentIndexAndNextTopic(string topic, TopicNames topicNames) {
        // if straight from the main topic, then use first retry
        if (topic == topicNames.Origin) {
            return (0, topicNames.Retries.Any() ? topicNames.Retries[0] : topicNames.DeadLetter);
        }

        // if any of the retries except the last, then use the next
        for (var i = 0; i < topicNames.Retries.Length - 1; i++) {
            if (topicNames.Retries[i] != topic) {
                continue;
            }

            var retryIndex = i + 1;
            return (retryIndex, topicNames.Retries[retryIndex]);
        }

        // otherwise dlq -- must have at least one 
        return (Math.Max(1, topicNames.Retries.Length + 1), topicNames.DeadLetter);
    }
}