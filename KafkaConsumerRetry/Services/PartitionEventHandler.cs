using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public class PartitionEventHandler : IPartitionEventHandler {
    private readonly ILogger _logger;
    private readonly IPartitionProcessorFactory _partitionProcessorFactory;
    private readonly IPartitionProcessorRepository _partitionProcessorRepository;
    private readonly IProducerFactory _producerFactory;

    public PartitionEventHandler(IPartitionProcessorFactory partitionProcessorFactory, IProducerFactory producerFactory, ILogger<PartitionEventHandler> logger,
        IPartitionProcessorRepository partitionProcessorRepository) {
        _logger = logger;
        _partitionProcessorRepository = partitionProcessorRepository;
        _producerFactory = producerFactory;
        _partitionProcessorFactory = partitionProcessorFactory;
    }



    /// <summary>
    ///     Stops and removes partition handlers when partitions are lost
    /// </summary>
    /// <param name="consumer"></param>
    /// <param name="list"></param>
    public virtual void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        _logger.LogInformation("LOST PARTITIONS {TopicPartitions}", list);
        foreach (var topicPartitionOffset in list) {
            _ = _partitionProcessorRepository.RemoveProcessorAsync(topicPartitionOffset.TopicPartition, RemovePartitionAction.Cancel);
        }
    }


    /// <summary>
    ///     Adds and starts partition handlers when partitions are assigned
    /// </summary>
    /// <param name="consumer"></param>
    /// <param name="consumerConfig"></param>
    /// <param name="list"></param>
    /// <param name="topicNames"></param>
    /// <param name="producerConfig"></param>
    public virtual void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartition> list,
        TopicNames topicNames, ProducerConfig producerConfig) {
        _logger.LogInformation("BEGIN ASSIGNED PARTITIONS {TopicPartitions}", list);

        // TODO: Figure out a way to reuse this retry producer across all relevant partitions
        var retryProducer = _producerFactory.BuildRetryProducer(producerConfig);
        foreach (var topicPartition in list) {
            var currentIndexAndNextTopic = GetCurrentIndexAndNextTopic(topicPartition.Topic, topicNames);
            var partitionProcessor = _partitionProcessorFactory.Create(consumer, topicPartition, currentIndexAndNextTopic.CurrentIndex,
                currentIndexAndNextTopic.NextTopic, consumerConfig.GroupId, retryProducer);
            _partitionProcessorRepository.AddProcessor(partitionProcessor, topicPartition);
        }

        _logger.LogInformation("END ASSIGNED PARTITIONS {TopicPartitions}", list);
    }

    /// <summary>
    ///     Waits till messages have been  and removes partition handlers when partitions are revoked
    /// </summary>
    /// <param name="consumer"></param>
    /// <param name="list"></param>
    public virtual void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        _logger.LogInformation("BEGIN REVOKED PARTITIONS {TopicPartitions}", list);
        List<Task> revokeWaiters = new();
        foreach (var topicPartitionOffset in list) {
            revokeWaiters.Add(_partitionProcessorRepository.RemoveProcessorAsync(topicPartitionOffset.TopicPartition, RemovePartitionAction.Revoke));
        }

        // Method needs to wait till the tasks are finished before revoking
        Task.WhenAll(revokeWaiters.ToArray()).ContinueWith(_ => _logger.LogInformation("END REVOKED PARTITIONS {TopicPartitions}", list)).GetAwaiter().GetResult();
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