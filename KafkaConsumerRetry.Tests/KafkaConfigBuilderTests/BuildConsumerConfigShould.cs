using FluentAssertions.Execution;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Tests.KafkaConfigBuilderTests; 

public class BuildConsumerConfigShould {
    [Fact]
    public void Set_Cluster_Defaults() {

        KafkaConfigBuilder kafkaConfigBuilder = new KafkaConfigBuilder();

        var config = kafkaConfigBuilder.BuildConsumerConfig(new Dictionary<string, string>());

        using (new AssertionScope()) {
            config.AutoOffsetReset.Should().Be(AutoOffsetReset.Earliest, "Should start from the earliest record where no offset is found");
            config.EnableAutoOffsetStore.Should().Be(false, "Offset should be stored manually to prevent messages being missed");
            config.EnableAutoCommit.Should().Be(true, "Auto commit should be true as this is fine to be periodic");
        }
    }
}