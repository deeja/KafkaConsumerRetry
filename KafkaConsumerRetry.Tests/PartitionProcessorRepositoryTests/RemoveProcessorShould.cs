using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace KafkaConsumerRetry.Tests.PartitionProcessorRepositoryTests;

public class RemoveProcessorShould {

    private readonly TopicPartition _topicPartition = new("origin", new Partition(2));

    [Fact]
    public async Task Cancel_Processor_When_Cancel_Selected() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);

        // adding the processor first
        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        await sut.RemoveProcessorAsync(_topicPartition, RemovePartitionAction.Cancel);

        mockPartitionProcessor.Verify(processor => processor.Cancel(), Times.Once());
    }


    [Fact]
    public async Task Revoke_Processor_When_Revoke_Selected() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);

        // adding the processor first
        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);

        await sut.RemoveProcessorAsync(_topicPartition, RemovePartitionAction.Revoke);

        mockPartitionProcessor.Verify(processor => processor.RevokeAsync(), Times.Once());
    }
    
    [Fact]
    public async Task Throw_When_Processor_Not_Found() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.RemoveProcessorAsync(_topicPartition, RemovePartitionAction.Revoke));
    }

    [Fact]
    public async Task Throw_When_Processor_Removed_Twice() {
        var mockRepository = new MockRepository(MockBehavior.Default);
        var mockPartitionProcessor = mockRepository.Create<IPartitionProcessor>();
        var mockLogger = mockRepository.Create<ILogger<PartitionProcessorRepository>>();
        var sut = new PartitionProcessorRepository(mockLogger.Object);

        // adding the processor first
        sut.AddProcessor(mockPartitionProcessor.Object, _topicPartition);
        await sut.RemoveProcessorAsync(_topicPartition, RemovePartitionAction.Revoke);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => sut.RemoveProcessorAsync(_topicPartition, RemovePartitionAction.Revoke));
    }
}