using Confluent.Kafka;
using KafkaConsumerRetry.DelayCalculators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public class PartitionProcessorFactory : IPartitionProcessorFactory {
    private readonly IServiceProvider _provider;

    public PartitionProcessorFactory(IServiceProvider provider) {
        _provider = provider;
    }

    public IPartitionProcessor Create(IConsumer<byte[], byte[]> consumer, TopicPartition topicPartition, int currentIndex, string nextTopic, string retryGroup,
        IProducer<byte[], byte[]> retryProducer) {
        // Can look at using ActivatorUtilities. Leaving as a constructor while building out things.
        var logger = _provider.GetRequiredService<ILogger<PartitionProcessor>>();
        var delayCalculator = _provider.GetRequiredService<IDelayCalculator>();
        var rateLimiter = _provider.GetRequiredService<IRateLimiter>();

        return new PartitionProcessor(logger, _provider, delayCalculator, rateLimiter, consumer, topicPartition, retryProducer, retryGroup, nextTopic, currentIndex);
    }
}