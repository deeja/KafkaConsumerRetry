using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry {
    public static class TypeRegistrationExtensions {
        public static IServiceCollection RegisterTypes(this IServiceCollection collection) {
            collection.AddSingleton<IDelayCalculator, SimpleBackOffDelayCalculator>()
                .AddSingleton<IConsumerFactory, ConsumerFactory>()
                .AddSingleton<ITopicPartitionQueueManager, TopicPartitionQueueManager>()
                .AddSingleton<ISupportTopicNameGenerator, SupportTopicNameGenerator>()
                .AddSingleton<IConsumerResultHandler, WriteToLoggerConsumerResultHandler>();

            return collection;
        }
    }
}