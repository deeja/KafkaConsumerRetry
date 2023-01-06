using System.Reflection;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Tests.Factories.ConsumerBuilderFactoryTests; 

public class CreateConsumerBuilderShould {
    [Fact]
    public void Return_ConsumerBuilder_With_Config_Set() {
        ConsumerBuilderFactory sut = new ConsumerBuilderFactory();
        var consumerConfig = new ConsumerConfig();
        var consumerBuilder = sut.CreateConsumerBuilder(consumerConfig);
        // Get config via reflection
        var value = consumerBuilder.GetType().GetProperty("Config",  BindingFlags.NonPublic | BindingFlags.Instance )!.GetValue(consumerBuilder)!;
        value.Should().BeSameAs(consumerConfig);
    }
}