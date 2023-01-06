using System.Reflection;
using KafkaConsumerRetry.Factories;
using Moq;

namespace KafkaConsumerRetry.Tests.Factories.ConsumerFactoryTests;

public class CreateProducerBuilder {
    
    [Fact]
    public void Build_Producer() {
        var producerConfig = new ProducerConfig();

        var sut = new ProducerBuilderFactory();

        var buildRetryProducer = sut.CreateProducerBuilder(producerConfig);
        // Get config via reflection
        var value = buildRetryProducer.GetType().GetProperty("Config",  BindingFlags.NonPublic | BindingFlags.Instance )!.GetValue(buildRetryProducer)!;
        value.Should().BeSameAs(producerConfig);
        
    }
    
    
}