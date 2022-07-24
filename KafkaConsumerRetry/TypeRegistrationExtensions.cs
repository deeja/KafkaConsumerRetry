using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaConsumerRetry {
    public static class TypeRegistrationExtensions {
        public static IServiceCollection AddKafkaConsumerRetry(this IServiceCollection collection) {
            return collection.AddSingleton<IDelayCalculator, BackOffDelayCalculator>()
                .AddSingleton<IProducerFactory, ProducerFactory>()
                .AddSingleton<IConsumerFactory, ConsumerFactory>()
                .AddSingleton<IReliableRetryRunner, ReliableRetryRunner>()
                .AddSingleton<ITopicPartitionQueueManager, TopicPartitionQueueManager>()
                .AddSingleton<ISupportTopicNameGenerator, SupportTopicNameGenerator>();
        }
    }
}