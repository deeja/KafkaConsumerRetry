using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Services;
using Moq;

namespace KafkaConsumerRetry.Tests.Factories.LocalConsumerBuilderTests;

public class BuildConsumerShould {

    private readonly ConsumerConfig _consumerConfig = new() {
        GroupId = "origin"
    };

    private readonly TopicNames _names = new("origin", new[] { "retry" }, "dlq");
    private readonly ProducerConfig _producerConfig = new();

    [Fact]
    public void Create_Consumer_Using_Config() {
        var repository = new MockRepository(MockBehavior.Strict);
        var messageManager = repository.Create<IPartitionMessageManager>();
        var consumerBuilderFactory = repository.Create<IConsumerBuilderFactory>();

        consumerBuilderFactory.Setup(factory => factory.CreateConsumerBuilder(_consumerConfig)).Returns(() => new ConsumerBuilder<byte[], byte[]>(_consumerConfig))
            .Verifiable();
        var sut = new LocalConsumerBuilder(messageManager.Object, consumerBuilderFactory.Object);

        var consumer = sut.BuildConsumer(_consumerConfig, _producerConfig, _names);
        consumer.Should().BeAssignableTo<IConsumer<byte[], byte[]>>();
    }
}