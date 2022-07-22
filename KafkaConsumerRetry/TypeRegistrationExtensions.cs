using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry {
    public static class TypeRegistrationExtensions {
        public static IServiceCollection RegisterTypes(this IServiceCollection collection) {
            collection.AddSingleton<IDelayCalculator, SimpleBackingOffDelayCalculator>()
                .AddSingleton<IConsumerFactory, ConsumerFactory>()
                .AddSingleton<ITopicManager, TopicManager>()
                .AddSingleton<ITopicPartitionQueueAllocator, TopicPartitionQueueAllocator>()
                .AddSingleton<ISupportTopicNameGenerator, SupportTopicNameGenerator>()
                .AddSingleton<IMessageValueHandler, MessageValueHandler>();

            return collection;
        }
    }
}