using Confluent.Kafka;
using ExampleProject;
using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;

var collection = new ServiceCollection();
const int maximumConcurrent = 10;
// Add default services
collection.AddKafkaConsumerRetry(maximumConcurrent, TimeSpan.FromSeconds(5));

collection.AddSingleton<IDeserializer<string>>(_ => Deserializers.Utf8);
collection.AddSingleton<IDeserializer<MyEvent>, MyEventDeserializer>();

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

var topicNaming = new TopicNaming();

// Example of a typed key/value handler
var myEventTopicNaming = topicNaming.GetTopicNaming("myevents.topic.example", kafkaRetryConfig);
var exampleTypedConsumerTask = requiredService.RunConsumersAsync<ExampleTypedHandler>(kafkaRetryConfig, myEventTopicNaming, CancellationToken.None);

// Example of a byte[] key/value handler
var byteTopicNaming = topicNaming.GetTopicNaming("mybytes.topic.example", kafkaRetryConfig);
var exampleByteConsumerTask = requiredService.RunConsumersAsync<ExampleByteHandler>(kafkaRetryConfig, byteTopicNaming, CancellationToken.None);

// await all tasks
await Task.WhenAll(exampleByteConsumerTask, exampleTypedConsumerTask);