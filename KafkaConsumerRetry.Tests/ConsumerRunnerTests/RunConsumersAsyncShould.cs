using KafkaConsumerRetry.Factories;
using KafkaConsumerRetry.Handlers;
using KafkaConsumerRetry.Services;
using Moq;

namespace KafkaConsumerRetry.Tests.ConsumerRunnerTests;

public class RunConsumersAsyncShould {

    [Fact]
    public async Task Subscribe_To_Origin_Topic() {
        var consumerFactory = new Mock<IConsumerFactory>();
        var messageManager = new Mock<IPartitionMessageManager>();
        var sut = new ConsumerRunner(consumerFactory.Object, messageManager.Object);
        var names = new TopicNames("origin", new[] { "retry" }, "dead_letter");
        var retryConfig = new KafkaRetryConfig();
        var originConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var retryConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var cancelledToken = new CancellationToken(true);

        consumerFactory.Setup(factory => factory.BuildOriginConsumer(retryConfig, names)).Returns(originConsumer.Object);
        consumerFactory.Setup(factory => factory.BuildRetryConsumer(retryConfig, names)).Returns(retryConsumer.Object);

        try {
            await sut.RunConsumersAsync<DummyHandler>(retryConfig, names, cancelledToken);
        }
        catch (TaskCanceledException) {
            // expected exception
        }

        originConsumer.Verify(consumer1 => consumer1.Subscribe(names.Origin));
    }

    [Fact]
    public async Task Subscribe_To_Retry_Topics() {
        var consumerFactory = new Mock<IConsumerFactory>();
        var messageManager = new Mock<IPartitionMessageManager>();
        var sut = new ConsumerRunner(consumerFactory.Object, messageManager.Object);
        var names = new TopicNames("origin", new[] { "retry" }, "dead_letter");
        var retryConfig = new KafkaRetryConfig();
        var originConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var retryConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var cancelledToken = new CancellationToken(true);

        consumerFactory.Setup(factory => factory.BuildOriginConsumer(retryConfig, names)).Returns(originConsumer.Object);
        consumerFactory.Setup(factory => factory.BuildRetryConsumer(retryConfig, names)).Returns(retryConsumer.Object);

        try {
            await sut.RunConsumersAsync<DummyHandler>(retryConfig, names, cancelledToken);
        }
        catch (TaskCanceledException) {
            // expected exception
        }

        retryConsumer.Verify(consumer1 => consumer1.Subscribe(names.Retries));
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class DummyHandler : IConsumerResultHandler {
    public Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}