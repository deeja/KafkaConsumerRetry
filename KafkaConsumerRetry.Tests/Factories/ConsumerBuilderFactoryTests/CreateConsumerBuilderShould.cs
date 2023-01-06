using System.Reflection;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Tests.Factories.ConsumerBuilderFactoryTests; 

public class CreateConsumerBuilderShould {
    [Fact]
    public void Return_ConsumerBuilder_With_Config_Set() {
        ConsumerBuilderFactory sut = new ConsumerBuilderFactory();
        var consumerConfig = new ConsumerConfig();
        var consumerBuilder = sut.CreateConsumerBuilder(consumerConfig);

        // Get config via reflection - kind of horrible, but can replace if ConsumerBuilder gets an interface
        var fieldInfo = consumerBuilder.GetType().GetField("_consumerBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
        var internalConsumerBuilder = fieldInfo.GetValue(consumerBuilder);
        var value = internalConsumerBuilder.GetType().GetProperty("Config",  BindingFlags.NonPublic | BindingFlags.Instance )!.GetValue(internalConsumerBuilder)!;
        value.Should().BeSameAs(consumerConfig);
    }
}