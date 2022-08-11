﻿using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public class PartitionManager : IPartitionManager {
    private readonly IProducerFactory _producerFactory;
    private readonly IDelayCalculator _delayCalculator;
    private readonly ILogger<PartitionManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<TopicPartition, PartitionProcessor> _partitionQueues = new();
    
    private readonly IRateLimiter _rateLimiter;
    private readonly IServiceProvider _serviceProvider;

    public PartitionManager(IProducerFactory producerFactory,
        IDelayCalculator delayCalculator, ILoggerFactory loggerFactory, IRateLimiter rateLimiter,
         IServiceProvider serviceProvider) {
        _producerFactory = producerFactory;
        _delayCalculator = delayCalculator;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLogger<PartitionManager>();
        _rateLimiter = rateLimiter;
        _serviceProvider = serviceProvider;
    }

    public void QueueConsumeResult<TResultHandler>(ConsumeResult<byte[], byte[]> consumeResult)
        where TResultHandler : IConsumerResultHandler {
        var topicPartition = consumeResult.TopicPartition;

        var partitionQueue = _partitionQueues[topicPartition];

        partitionQueue.Enqueue<TResultHandler>(consumeResult);
    }

    public void HandleLostPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartitionOffset> list) {
        using (_logger.BeginScope(nameof(HandleLostPartitions))) {
            _logger.LogInformation("LOST PARTITIONS {TopicPartitions}", list);

            foreach (var topicPartitionOffset in list) {
                _partitionQueues[topicPartitionOffset.TopicPartition].Cancel();
                _partitionQueues.Remove(topicPartitionOffset.TopicPartition);
            }
        }
    }

    public void HandleAssignedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartition> list,
        TopicNaming topicNaming, ProducerConfig producerConfig) {
        _logger.LogInformation("ASSIGNED PARTITIONS {TopicPartitions}", list);
        // get the group id from the setting for the retry -- need a better way of doing this
        var retryGroupId = consumerConfig.GroupId;

        var retryProducer = _producerFactory.BuildRetryProducer(producerConfig);
        // TODO: this is not great. shouldn't return the current index and next topic
        foreach (var topicPartition in list) {
            var currentIndexAndNextTopic = GetCurrentIndexAndNextTopic(topicPartition.Topic, topicNaming);
            _partitionQueues.Add(topicPartition,
                new PartitionProcessor(_loggerFactory, _serviceProvider, _delayCalculator, _rateLimiter, consumer,
                    topicPartition,
                    retryProducer, retryGroupId, currentIndexAndNextTopic.NextTopic,
                    currentIndexAndNextTopic.CurrentIndex));
        }
    }

    public void HandleRevokedPartitions(IConsumer<byte[], byte[]> consumer, ConsumerConfig consumerConfig,
        List<TopicPartitionOffset> list) {
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
        if (topic == topicNaming.Origin)
            return (0, topicNaming.Retries.Any() ? topicNaming.Retries[0] : topicNaming.DeadLetter);

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