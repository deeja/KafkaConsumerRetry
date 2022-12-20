using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry;

public static class TypeRegistrationExtensions {
    /// <summary>
    /// Add Kafka Consumer Retry default services
    /// </summary>
    /// <param name="collection">Service Collection</param>
    /// <param name="maximumConcurrent">Maximum concurrent message processors. This applies across all consumers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection, int maximumConcurrent) {
        return collection.AddSingleton<IDelayCalculator, MultiplyingBackOffCalculator>()
            .AddSingleton<IProducerFactory, ProducerFactory>()
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IRateLimiter>(_ => new RateLimiter(maximumConcurrent))
            .AddSingleton<IConsumerRunner, ConsumerRunner>()
            .AddSingleton<IPartitionMessageManager, PartitionMessageManager>()
            .AddSingleton<ITopicNaming, TopicNaming>();
    }
}