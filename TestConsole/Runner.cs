using System.Globalization;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Handlers;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;

namespace TestConsole;

public class Runner {
    private readonly IConsumerRunner _consumerRunner;
    private readonly ITopicNaming _naming;
    private readonly string _topicName = "console_runner.topic." + Timestamp.DateTimeToUnixTimestampMs(DateTime.Now);

    public Runner(IConsumerRunner consumerRunner, ITopicNaming naming) {
        _consumerRunner = consumerRunner;
        _naming = naming;
    }

    public async Task ExecuteAsync(int messageCount, CancellationToken cancellationToken, int partitions = 12, int retryAttempts = 3) {
        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedCancellationToken = cancellationTokenSource.Token;
        var waitForAnyKeyAsync = WaitForAnyKeyAsync(cancellationTokenSource);

        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        Console.WriteLine("--- Creating Topics ---");

        await CreateTopicsAsync(partitions, retryAttempts);
        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        Console.WriteLine("--- Generating messages ---");
        await GenerateMessagesAsync(messageCount, linkedCancellationToken);
        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        Console.WriteLine("--- Starting Consumers ---");
        var retryServiceConfig = new KafkaRetryConfig {
            RetryAttempts = retryAttempts,
            OriginCluster = new Dictionary<string, string> {
                ["group.id"] = "my-group-name",
                ["bootstrap.servers"] = "localhost:9092",
                ["client.id"] = "client-id"
            }
        };
        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        var topicNaming = _naming.GetTopicNaming(_topicName, retryServiceConfig);
        if (cancellationToken.IsCancellationRequested) {
            return;
        }

        var consumerTask = _consumerRunner.RunConsumersAsync<IConsumerResultHandler>;

        await Task.WhenAny(consumerTask(retryServiceConfig, topicNaming, linkedCancellationToken), waitForAnyKeyAsync);
    }

    private async Task WaitForAnyKeyAsync(CancellationTokenSource tokenSource) {
        await Task.Yield();
        Console.WriteLine("--- Press any key to quit ---");
        while (true) {
            await Task.Delay(200, tokenSource.Token);
            if (Console.KeyAvailable) {
                break;
            }
        }

        Console.WriteLine("--- Cancelling ---");
        tokenSource.Cancel();
    }

    private async Task GenerateMessagesAsync(int numberOfMessages, CancellationToken cancellationToken) {
        await Task.Yield();
        var producer =
            new ProducerBuilder<byte[], byte[]>(new ProducerConfig { BootstrapServers = "localhost:9092" })
                .Build();
        Console.WriteLine($"Producing {numberOfMessages} messages on {_topicName}");
        for (var i = 0; i < numberOfMessages; i++) {
            var messageValue = i % 5 == 0 ? "THROW" : "WAIT";
            var message = new Message<byte[], byte[]> {
                Key = Encoding.UTF8.GetBytes($"{Guid.NewGuid()}-{i}-{DateTime.Now.ToString(CultureInfo.InvariantCulture)}"),
                Value = Encoding.UTF8.GetBytes(messageValue)
            };
            var deliveryResult = await producer.ProduceAsync(_topicName, message, cancellationToken);
        }
    }

    private async Task CreateTopicsAsync(int partitions, int retries) {
        AdminClientBuilder clientBuilder = new(new AdminClientConfig {
            BootstrapServers = "localhost:9092"
        });

        var adminClient = clientBuilder.Build();

        foreach (var topic in GetTopicNames(retries)) {
            try {
                Console.WriteLine($"Creating: {topic} - Partitions: {partitions}");
                await adminClient.CreateTopicsAsync(new TopicSpecification[] { new() { Name = topic, NumPartitions = partitions } });
            }
            catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }

    private IEnumerable<string> GetTopicNames(int retries) {
        yield return $"{_topicName}";
        for (var i = 0; i < retries; i++) {
            yield return $"{_topicName}.retry.{i}";
        }

        yield return $"{_topicName}.dlq";
    }
}