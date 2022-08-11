using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry;

public static class TypeRegistrationExtensions {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="collection">Service Collection</param>
    /// <param name="maximumConcurrent">Maximum concurrent message processors. This applies across all consumers</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection, int maximumConcurrent) {
        return collection.AddSingleton<IDelayCalculator, IncrementalBackOffDelayCalculator>()
            .AddSingleton<IProducerFactory, ProducerFactory>()
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IRateLimiter>(_ => new RateLimiter(maximumConcurrent))
            .AddSingleton<IConsumerRunner, ConsumerRunner>()
            .AddSingleton<IPartitionManager, PartitionManager>()
            .AddSingleton<ITopicNaming, TopicNaming>();
    }
}