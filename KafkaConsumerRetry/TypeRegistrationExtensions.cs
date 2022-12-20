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
    /// <param name="maximumConcurrentTasks">Maximum concurrent message processors. This applies across all consumers</param>
    /// <param name="delayBase">Base retry time used by the <see cref="MultiplyingBackOffCalculator"/></param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection, int maximumConcurrentTasks, TimeSpan delayBase) {
        return collection.AddSingleton<IDelayCalculator>(_ => new MultiplyingBackOffCalculator(delayBase))
            .AddSingleton<IProducerFactory, ProducerFactory>()
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IRateLimiter>(_ => new RateLimiter(maximumConcurrentTasks))
            .AddSingleton<IConsumerRunner, ConsumerRunner>()
            .AddSingleton<IPartitionMessageManager, PartitionMessageManager>()
            .AddSingleton<ITopicNaming, TopicNaming>();
    }
}