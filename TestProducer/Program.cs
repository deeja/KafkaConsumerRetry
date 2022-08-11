// See https://aka.ms/new-console-template for more information

using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.SupportTopicNaming;

Console.WriteLine("Message producer");

var bootstrap = Environment.GetEnvironmentVariable("BOOTSTRAP") ?? "localhost:9092";
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "test-producer";
var mainTopic = Environment.GetEnvironmentVariable("TOPIC") ?? "test";
Console.WriteLine($"Bootstrap: {bootstrap}");
Console.WriteLine($"Client Id: {clientId}");
Console.WriteLine($"Topic: {mainTopic}");
await CreateTopicsAsync();
await GenerateMessagesAsync();

async Task GenerateMessagesAsync() {
    await Task.Yield();
    var producer =
        new ProducerBuilder<byte[], byte[]>(new ProducerConfig { BootstrapServers = bootstrap, ClientId = clientId })
            .Build();

    for (var i = 0; i < 1000; i++) {
        var messageValue = i % 5 == 0 ? "THROW" : "WAIT";
        var message = new Message<byte[], byte[]> {
            Key = Encoding.UTF8.GetBytes(i + "-" + DateTime.Now.ToString(CultureInfo.InvariantCulture)),
            Value = Encoding.UTF8.GetBytes(messageValue)
        };
        await producer.ProduceAsync(mainTopic, message);
    }
}

async Task CreateTopicsAsync() {
    AdminClientBuilder clientBuilder = new(new AdminClientConfig {
        BootstrapServers = bootstrap
    });

    var adminClient = clientBuilder.Build();

    var topicNaming = new TopicNameGenerator(new RetryServiceConfig { RetryAttempts = 3 }).GetTopicNaming(mainTopic);

    var allTopics = new[] { topicNaming.Origin, topicNaming.DeadLetter }.Concat(topicNaming.Retries);
    
    foreach (var t in allTopics) {
        try {
            await adminClient.CreateTopicsAsync(new TopicSpecification[]
                { new() { Name = t, NumPartitions = 12 } });
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }
}