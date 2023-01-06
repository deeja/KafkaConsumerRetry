using KafkaConsumerRetry.Factories;
using Moq;

namespace KafkaConsumerRetry.Tests.Factories.ProducerFactoryTests; 

public class BuildRetryProducerShould {
    [Fact]
    public void Build_Producer_From_Config() {
        var mockRepository = new MockRepository(MockBehavior.Strict);
        var builderFactoryMock = mockRepository.Create<IProducerBuilderFactory>();

        var producerConfig = new ProducerConfig();
        var producerBuilder = new ProducerBuilder<byte[], byte[]>(producerConfig);

        builderFactoryMock.Setup(factory => factory.CreateProducerBuilder(producerConfig)).Returns(() => producerBuilder);
        
        var sut = new ProducerFactory(builderFactoryMock.Object);
        var retryProducer = sut.BuildRetryProducer(producerConfig);

        retryProducer.Should().BeAssignableTo<IProducer<byte[], byte[]>>();
        
        mockRepository.Verify();
        
        
        
        
    }
}