using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KafkaConsumerRetry.Tests.PartitionProcessorRepositoryTests;

public class AddProcessorShould {


    readonly TopicPartition _topicPartition = new("origin", new Partition(2));

    [Fact]
    public void Start_Processor() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);

        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        mockPartitionProcessor.Verify(processor => processor.Start(), Times.Once());
    }

    [Fact]
    public void Throw_When_Processor_Already_Exists_For_Partition() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);

        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        Assert.Throws<ArgumentException>(() => sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition));
    }
}