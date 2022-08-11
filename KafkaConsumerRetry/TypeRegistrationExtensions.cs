using KafkaConsumerRetry.DelayCalculators;
using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry;

public static class TypeRegistrationExtensions {
    public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection) {
        return collection.AddSingleton<IDelayCalculator, BackOffDelayCalculator>()
            .AddSingleton<IProducerFactory, ProducerFactory>()
            .AddSingleton<IConsumerFactory, ConsumerFactory>()
            .AddSingleton<IRateLimiter, RateLimiter>()
            .AddSingleton<IConsumerRunner, ConsumerRunner>()
            .AddSingleton<IPartitionManager, PartitionManager>()
            .AddSingleton<ITopicNameGenerator, TopicNameGenerator>();
    }
}