using FluentAssertions.Execution;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Tests.KafkaConfigBuilderTests; 

public class BuildProducerConfigShould {
    [Fact]
    public void Return_ProducerConfig() {

        KafkaConfigBuilder kafkaConfigBuilder = new KafkaConfigBuilder();

        var config = kafkaConfigBuilder.BuildProducerConfig(new Dictionary<string, string>());

        config.Should().NotBeNull();
    }
}