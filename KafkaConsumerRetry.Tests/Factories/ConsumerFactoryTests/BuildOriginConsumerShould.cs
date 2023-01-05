using KafkaConsumerRetry.Factories;
using Moq;

namespace KafkaConsumerRetry.Tests.Factories.ConsumerFactoryTests;

public class BuildOriginConsumerShould {


    [Fact]
    public void Build_Consumer() {
        var localConsumerBuilder = new Mock<ILocalConsumerBuilder>();
        var consumerMock = new Mock<IConsumer<byte[], byte[]>>();
        var configBuilderMock = new Mock<KafkaConfigBuilder>();
        var consumerConfig = new ConsumerConfig();
        var producerConfig = new ProducerConfig();

        configBuilderMock.Setup(builder => builder.BuildConsumerConfig(It.IsAny<IDictionary<string, string>>())).Returns(() => consumerConfig);
        configBuilderMock.Setup(builder => builder.BuildProducerConfig(It.IsAny<IDictionary<string, string>>())).Returns(() => producerConfig);
        

        var topicNames = new TopicNames("origin", new[] { "retry" }, "dlq");
        localConsumerBuilder.Setup(builder => builder.BuildConsumer(It.IsAny<ConsumerConfig>(), It.IsAny<ProducerConfig>(), It.IsAny<TopicNames>()))
            .Returns(() => consumerMock.Object);

        var sut = new ConsumerFactory(localConsumerBuilder.Object, configBuilderMock.Object);

        var kafkaRetryConfig = new KafkaRetryConfig {
            RetryAttempts = 2,
            OriginCluster = new Dictionary<string, string>(),
            RetryCluster = null
        };

        sut.BuildOriginConsumer(kafkaRetryConfig, topicNames);
        
        localConsumerBuilder.Verify(builder => builder.BuildConsumer(consumerConfig, producerConfig, topicNames));
    }
}