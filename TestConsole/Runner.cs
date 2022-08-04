using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using TopicNaming = KafkaConsumerRetry.Configuration.TopicNaming;

namespace TestConsole {
    public class Runner {
        private readonly ITopicNaming _naming;
        private readonly IReliableRetryRunner _reliableRetryRunner;

        public Runner(IReliableRetryRunner reliableRetryRunner, ITopicNaming naming) {
            _reliableRetryRunner = reliableRetryRunner;
            _naming = naming;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken) {
            string originalName = "my.topic";
            TopicNaming topicNaming = _naming.GetTopicNaming(originalName);
            await CreateTopicsAsync();
            _ = GenerateMessagesAsync();

            await _reliableRetryRunner.RunConsumersAsync(topicNaming, cancellationToken);
        }

        private async Task GenerateMessagesAsync() {
            await Task.Yield();
            var producer =
                new ProducerBuilder<byte[], byte[]>(new ProducerConfig {BootstrapServers = "localhost:9092"})
                    .Build();

            for (var i = 0; i < 1; i++) {
                await producer.ProduceAsync("my.topic", new Message<byte[], byte[]> {
                    Key = Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture)),
                    Value = Encoding.UTF8.GetBytes(DateTime.Now.ToString(CultureInfo.InvariantCulture))
                });
            }
        }

        private async Task CreateTopicsAsync() {
            AdminClientBuilder clientBuilder = new(new AdminClientConfig {
                BootstrapServers = "localhost:9092"
            });

            var adminClient = clientBuilder.Build();

            var topics = new[] {"my.topic", "my.topic.retry.0", "my.topic.retry.1", "my.topic.retry.2", "my.topic.dlq"};
            foreach (var topic in topics) {
                try {
                    await adminClient.CreateTopicsAsync(new TopicSpecification[]
                        {new() {Name = topic, NumPartitions = 12}});
                }
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }
    }
}