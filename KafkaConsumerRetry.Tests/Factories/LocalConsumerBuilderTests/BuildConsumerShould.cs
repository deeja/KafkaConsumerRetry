using System.Reflection;
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
        var repository = new MockRepository(MockBehavior.Default);
        var messageManager = repository.Create<IPartitionMessageManager>();
        var consumerBuilderFactory = repository.Create<IConsumerBuilderFactory>();
        var mockConsumer = repository.Create<IConsumer<byte[], byte[]>>();

        consumerBuilderFactory.Setup(factory => factory.CreateConsumerBuilder(_consumerConfig).Build()).Returns(() => mockConsumer.Object);

        var sut = new LocalConsumerFactory(messageManager.Object, consumerBuilderFactory.Object);

        var consumer = sut.BuildConsumer(_consumerConfig, _producerConfig, _names);

        repository.VerifyAll();
        consumer.Should().BeSameAs(mockConsumer.Object);
    }

    [Fact]
    public void Set_Partition_AssignedHandler() {
        var repository = new MockRepository(MockBehavior.Loose);
        var messageManager = repository.Create<IPartitionMessageManager>();
        var consumerBuilderFactory = repository.Create<IConsumerBuilderFactory>();
        var consumerBuilderMock = repository.Create<IConsumerBuilder>();

        // Setup the call to get the action assigned to the PartitionAssignedHandler
        consumerBuilderFactory.Setup(factory => factory.CreateConsumerBuilder(_consumerConfig)).Returns(() => consumerBuilderMock.Object);
        Action<IConsumer<byte[], byte[]>, List<TopicPartition>> partitionAssignedAction = null!;
        consumerBuilderMock.Setup(builder => builder.SetPartitionsAssignedHandler(It.IsAny<Action<IConsumer<byte[], byte[]>, List<TopicPartition>>>()))
            .Callback((Action<IConsumer<byte[], byte[]>, List<TopicPartition>> action) => partitionAssignedAction = action);
        
        var sut = new LocalConsumerFactory(messageManager.Object, consumerBuilderFactory.Object);
        var consumer = sut.BuildConsumer(_consumerConfig, _producerConfig, _names);

        partitionAssignedAction.Should().NotBeNull();
        
        // Call the action to see if it calls the message manager
        List<TopicPartition> topicPartitions = new List<TopicPartition>();
        partitionAssignedAction(consumer, topicPartitions);
        
        // Verify messagemanager was called
        messageManager.Verify(manager => manager.HandleAssignedPartitions(consumer, _consumerConfig, topicPartitions, _names, _producerConfig));
    }
    
    [Fact]
    public void Set_PartitionLostHandler() {
        var repository = new MockRepository(MockBehavior.Loose);
        var messageManager = repository.Create<IPartitionMessageManager>();
        var consumerBuilderFactory = repository.Create<IConsumerBuilderFactory>();
        var consumerBuilderMock = repository.Create<IConsumerBuilder>();

        // Setup the call to get the action assigned to the handler
        consumerBuilderFactory.Setup(factory => factory.CreateConsumerBuilder(_consumerConfig)).Returns(() => consumerBuilderMock.Object);
        Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionLostAction = null!;
        consumerBuilderMock.Setup(builder => builder.SetPartitionsLostHandler(It.IsAny<Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>>>()))
            .Callback((Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> action) => partitionLostAction = action);
        
        var sut = new LocalConsumerFactory(messageManager.Object, consumerBuilderFactory.Object);
        var consumer = sut.BuildConsumer(_consumerConfig, _producerConfig, _names);

        partitionLostAction.Should().NotBeNull();
        
        // Call the action to see if it calls the message manager
        List<TopicPartitionOffset> topicPartitions = new List<TopicPartitionOffset>();
        partitionLostAction(consumer, topicPartitions);
        
        // Verify messagemanager was called
        messageManager.Verify(manager => manager.HandleLostPartitions(consumer, topicPartitions));
    }
    
    [Fact]
    public void Set_PartitionsRevokedHandler() {
        var repository = new MockRepository(MockBehavior.Loose);
        var messageManager = repository.Create<IPartitionMessageManager>();
        var consumerBuilderFactory = repository.Create<IConsumerBuilderFactory>();
        var consumerBuilderMock = repository.Create<IConsumerBuilder>();

        // Setup the call to retrieve the action assigned to the handler
        consumerBuilderFactory.Setup(factory => factory.CreateConsumerBuilder(_consumerConfig)).Returns(() => consumerBuilderMock.Object);
        Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsRevokedAction = null!;
        consumerBuilderMock.Setup(builder => builder.SetPartitionsRevokedHandler(It.IsAny<Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>>>()))
            .Callback((Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> action) => partitionsRevokedAction = action);
        
        var sut = new LocalConsumerFactory(messageManager.Object, consumerBuilderFactory.Object);
        var consumer = sut.BuildConsumer(_consumerConfig, _producerConfig, _names);

        partitionsRevokedAction.Should().NotBeNull();
        
        // Call the action to see if it calls the message manager
        List<TopicPartitionOffset> topicPartitions = new List<TopicPartitionOffset>();
        partitionsRevokedAction(consumer, topicPartitions);
        
        // Verify messagemanager was called
        messageManager.Verify(manager => manager.HandleRevokedPartitions(consumer, topicPartitions));
    }
}