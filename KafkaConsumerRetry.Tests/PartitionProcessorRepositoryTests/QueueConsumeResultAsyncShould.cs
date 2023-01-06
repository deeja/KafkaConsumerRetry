using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.Tests.ConsumerRunnerTests;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KafkaConsumerRetry.Tests.PartitionProcessorRepositoryTests;

public class QueueConsumeResultAsyncShould {

    private readonly NullLogger<PartitionProcessorRepository> _nullLogger = new();
    private readonly TopicPartition _topicPartition = new("origin", new Partition(2));

    [Fact]
    public async Task Enqueue_On_Processor() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var sut = new PartitionProcessorRepository(_nullLogger);

        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        var consumeResult = new ConsumeResult<byte[], byte[]> {
            TopicPartitionOffset = new TopicPartitionOffset(_topicPartition, 0)
        };

        await sut.QueueConsumeResultAsync<DummyHandler>(consumeResult);

        mockPartitionProcessor.Verify(processor => processor.Enqueue<DummyHandler>(consumeResult), Times.Once);
    }



    /// <summary>
    ///     Sometimes the message gets to the consumer before we have time to add the processor
    /// </summary>
    [Fact]
    public async Task Enqueue_On_Processor_When_Addition_Of_Processor_Is_Delayed() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var sut = new PartitionProcessorRepository(_nullLogger);

        var consumeResult = new ConsumeResult<byte[], byte[]> {
            TopicPartitionOffset = new TopicPartitionOffset(_topicPartition, 0)
        };

        var queueTask = sut.QueueConsumeResultAsync<DummyHandler>(consumeResult);
        await Task.Delay(200);
        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        await queueTask;

        mockPartitionProcessor.Verify(processor => processor.Enqueue<DummyHandler>(consumeResult));
    }

    /// <summary>
    ///     Sometimes the message gets to the consumer before we have time to add the processor
    /// </summary>
    [Fact(Timeout = 2000)]
    public async Task Throw_If_Enqueue_Called_But_No_Processor_Added_In_Time() {
        var sut = new PartitionProcessorRepository(_nullLogger);

        var consumeResult = new ConsumeResult<byte[], byte[]> {
            TopicPartitionOffset = new TopicPartitionOffset(_topicPartition, 0)
        };

        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.QueueConsumeResultAsync<DummyHandler>(consumeResult));
    }
}