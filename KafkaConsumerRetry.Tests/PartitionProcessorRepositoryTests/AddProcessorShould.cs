using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace KafkaConsumerRetry.Tests.PartitionProcessorRepositoryTests;

public class AddProcessorShould {
    private readonly NullLogger<PartitionProcessorRepository> _nullLogger = new();
    private readonly TopicPartition _topicPartition = new("origin", new Partition(2));

    [Fact]
    public void Start_Processor() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var sut = new PartitionProcessorRepository(_nullLogger);

        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        mockPartitionProcessor.Verify(processor => processor.Start(), Times.Once());
    }

    [Fact]
    public void Throw_When_Processor_Already_Exists_For_Partition() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var sut = new PartitionProcessorRepository(_nullLogger);

        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        Assert.Throws<ArgumentException>(() => sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition));
    }
}