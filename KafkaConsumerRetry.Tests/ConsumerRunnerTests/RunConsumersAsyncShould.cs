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
    
    [Fact(Timeout = 500)]
    public async Task Move_Consumed_Messages_To_PartitionMessageManager() {
        var consumerFactory = new Mock<IConsumerFactory>();
        var messageManager = new Mock<IPartitionMessageManager>();
        var sut = new ConsumerRunner(consumerFactory.Object, messageManager.Object);
        var names = new TopicNames("origin", new[] { "retry" }, "dead_letter");
        var retryConfig = new KafkaRetryConfig();
        var originConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var retryConsumer = new Mock<IConsumer<byte[], byte[]>>();
        var tokenSource = new CancellationTokenSource();
        var consumeResult = new ConsumeResult<byte[], byte[]>();

        consumerFactory.Setup(factory => factory.BuildOriginConsumer(retryConfig, names)).Returns(originConsumer.Object);
        consumerFactory.Setup(factory => factory.BuildRetryConsumer(retryConfig, names)).Returns(retryConsumer.Object);

        var tokenSourceToken = tokenSource.Token;


        originConsumer.Setup(consumer => consumer.Consume(tokenSourceToken)).Returns(consumeResult);
        messageManager.Setup(manager => manager.QueueConsumeResult<DummyHandler>(consumeResult)).Callback(() => tokenSource.Cancel());

        try {
            await sut.RunConsumersAsync<DummyHandler>(retryConfig, names, tokenSourceToken);
        }
        catch (TaskCanceledException) {
            // expected exception
        }
        
        messageManager.Verify(manager => manager.QueueConsumeResult<DummyHandler>(consumeResult));

        
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
internal class DummyHandler : IConsumerResultHandler {
    public Task HandleAsync(ConsumeResult<byte[], byte[]> consumeResult, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}