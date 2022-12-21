using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public sealed class PartitionProcessor : PartitionProcessorBase {

    public PartitionProcessor(ILogger<PartitionProcessor> logger, IServiceProvider serviceProvider, IDelayCalculator delayCalculator, IRateLimiter rateLimiter,
        IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition, IProducer<byte[], byte[]> retryProducer, string retryGroupId, string nextTopic,
        int retryIndex) : base(logger, serviceProvider, delayCalculator, rateLimiter, consumer, topicPartition, retryProducer, retryGroupId, nextTopic,
        retryIndex) { }
}