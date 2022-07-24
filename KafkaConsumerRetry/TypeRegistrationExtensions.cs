using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry {
    public static class TypeRegistrationExtensions {
        public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection) {
            return collection.AddSingleton<IDelayCalculator, SimpleBackOffDelayCalculator>()
                .AddSingleton<IConsumerFactory, ConsumerFactory>()
                .AddSingleton<ITopicPartitionQueueManager, TopicPartitionQueueManager>()
                .AddSingleton<ISupportTopicNameGenerator, SupportTopicNameGenerator>()
                .AddSingleton<IConsumerResultHandler, WriteToLoggerConsumerResultHandler>();
        }
    }
}