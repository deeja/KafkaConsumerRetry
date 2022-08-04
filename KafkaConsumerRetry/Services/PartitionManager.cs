using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public class PartitionManager : IPartitionManager {
    private readonly RetryServiceConfig _config;
    private readonly IDelayCalculator _delayCalculator;
    private readonly IConsumerResultHandler _handler;
    private readonly ILogger<PartitionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<TopicPartition, PartitionProcessor> _partitionQueues = new();
    private readonly IProducer<byte[], byte[]> _producer;
    private readonly IRateLimiter _rateLimiter;

    public PartitionManager(IConsumerResultHandler handler, IProducerFactory producer,
        IDelayCalculator delayCalculator, ILoggerFactory loggerFactory, IRateLimiter rateLimiter,
        RetryServiceConfig config) {
        _producer = producer.BuildRetryProducer();
        _handler = handler;
        _delayCalculator = delayCalculator;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<PartitionManager>();
        _rateLimiter = rateLimiter;
        _config = config;
    }

    public void QueueConsumeResult(ConsumeResult<byte[], byte[]> consumeResult) {
        var topicPartition = consumeResult.TopicPartition;

        var partitionQueue = _partitionQueues[topicPartition];

        partitionQueue.Enqueue(consumeResult);
    }

    public void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        using (_logger.BeginScope(nameof(HandleLostPartitions))) {
            _logger.LogInformation("LOST PARTITIONS {TopicPartitions}", list);

            foreach (var topicPartitionOffset in list) {
                _partitionQueues[topicPartitionOffset.TopicPartition].Cancel();
                _partitionQueues.Remove(topicPartitionOffset.TopicPartition);
            }
        }
    }

    public void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartition> list,
        TopicNaming topicNaming) {
        _logger.LogInformation("ASSIGNED PARTITIONS {TopicPartitions}", list);
        // get the group id from the setting for the retry -- need a better way of doing this
        var retryGroupId = (_config.RetryKafka ?? _config.TopicKafka)["group.id"];

        // TODO: this is not great. shouldn't return the current index and next topic
        foreach (var topicPartition in list) {
            var currentIndexAndNextTopic = GetCurrentIndexAndNextTopic(topicPartition.Topic, topicNaming);
            _partitionQueues.Add(topicPartition,
                new PartitionProcessor(_handler, _loggerFactory, _delayCalculator, _rateLimiter, consumer,
                    topicPartition,
                    _producer, retryGroupId, currentIndexAndNextTopic.NextTopic,
                    currentIndexAndNextTopic.CurrentIndex));
        }
    }

    public void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, List<TopicPartitionOffset> list) {
        _logger.LogInformation("BEGIN REVOKED PARTITIONS {TopicPartitions}", list);
        List<Task> revokeWaiters = new();
        foreach (var topicPartitionOffset in list) {
            var topicPartition = topicPartitionOffset.TopicPartition;
            var partitionQueue = _partitionQueues[topicPartition];
            _partitionQueues.Remove(topicPartition);
            revokeWaiters.Add(partitionQueue.RevokeAsync());
        }

        // TODO: Something better than this waiter
        Task.Run(() => Task.WaitAll(revokeWaiters.ToArray())).GetAwaiter().GetResult();
        _logger.LogInformation("END REVOKED PARTITIONS {TopicPartitions}", list);
    }

    /// <summary>
    ///     Gets the current index of the topic, and also the next topic to push to
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="topicNaming"></param>
    /// <returns></returns>
    private (int CurrentIndex, string NextTopic)
        GetCurrentIndexAndNextTopic(string topic, TopicNaming topicNaming) {
        // if straight from the main topic, then use first retry
        if (topic == topicNaming.Origin) {
            return (0, topicNaming.Retries.Any() ? topicNaming.Retries[0] : topicNaming.DeadLetter);
        }

        // if any of the retries except the last, then use the next
        for (var i = 0; i < topicNaming.Retries.Length - 1; i++) {
            if (topicNaming.Retries[i] == topic) {
                var retryIndex = i + 1;
                return (retryIndex, topicNaming.Retries[retryIndex]);
            }
        }

        // otherwise dlq -- must have at least one 
        return (Math.Max(1, topicNaming.Retries.Length + 1), topicNaming.DeadLetter);
    }
}