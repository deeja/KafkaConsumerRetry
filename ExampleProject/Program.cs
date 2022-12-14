// See https://aka.ms/new-console-template for more information

using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using Microsoft.Extensions.DependencyInjection;

var collection = new ServiceCollection();
const int maximumConcurrent = 10;
// Add default services
collection.AddKafkaConsumerRetry(maximumConcurrent);

var serviceProvider = collection.BuildServiceProvider();

var requiredService = serviceProvider.GetRequiredService<IConsumerRunner>();

var kafkaRetryConfig = new KafkaRetryConfig {
    RetryAttempts = 3,
    OriginCluster = new Dictionary<string, string> {
        ["group.id"] = "my-group-name",
        ["bootstrap.servers"] = "origin.localhost:9092",
        ["client.id"] = "client-id"
    },
    // OPTIONAL: 
    RetryCluster = new Dictionary<string, string> {
        ["group.id"] = "my-group-name",
        ["bootstrap.servers"] = "local.localhost:9092",
        ["client.id"] = "client-id"
    }
};

var topicNaming = new TopicNaming().GetTopicNaming("mytopic", kafkaRetryConfig);
await requiredService.RunConsumersAsync<MyCustomHandler>(kafkaRetryConfig, topicNaming, CancellationToken.None);