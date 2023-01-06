using KafkaConsumerRetry.Factories;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using Moq;

namespace KafkaConsumerRetry.Tests.Factories.ConsumerFactoryTests;

public class BuildRetryConsumerShould {

    private readonly TopicNames _topicNames = new("origin", new[] { "retry" }, "dlq");

    [Fact]
    public void Build_Consumer() {
        var localConsumerBuilder = new Mock<ILocalConsumerFactory>();
        var consumerMock = new Mock<IConsumer<byte[], byte[]>>();
        var configBuilderMock = new Mock<KafkaConfigBuilder>();
        var consumerConfig = new ConsumerConfig();
        var producerConfig = new ProducerConfig();

        configBuilderMock.Setup(builder => builder.BuildConsumerConfig(It.IsAny<IDictionary<string, string>>())).Returns(() => consumerConfig);
        configBuilderMock.Setup(builder => builder.BuildProducerConfig(It.IsAny<IDictionary<string, string>>())).Returns(() => producerConfig);

        localConsumerBuilder.Setup(builder => builder.BuildConsumer(consumerConfig, producerConfig, _topicNames))
            .Returns(() => consumerMock.Object);

        var sut = new ConsumerFactory(localConsumerBuilder.Object, configBuilderMock.Object);

        var kafkaRetryConfig = new KafkaRetryConfig {
            RetryAttempts = 2,
            OriginCluster = new Dictionary<string, string>(),
            RetryCluster = null
        };

        sut.BuildRetryConsumer(kafkaRetryConfig, _topicNames);

        localConsumerBuilder.Verify(builder => builder.BuildConsumer(consumerConfig, producerConfig, _topicNames));
    }
    
    
    /// <summary>
    ///     Make sure the correct settings are used where retry is supplied
    /// </summary>
    [Fact]
    public void Use_Origin_Settings_For_Consumer_And_Retry_Where_No_Retry_Specified() {
        var mockFactory = new MockRepository(MockBehavior.Strict);
        var localConsumerBuilder = mockFactory.Create<ILocalConsumerFactory>();
        var consumerMock = mockFactory.Create<IConsumer<byte[], byte[]>>();
        var configBuilderMock = mockFactory.Create<KafkaConfigBuilder>();
        var consumerConfig = new ConsumerConfig();
        var producerConfig = new ProducerConfig();

        var originCluster = new Dictionary<string, string> {
            ["group.id"] = "origin"
        };
        
        var config = new KafkaRetryConfig {
            OriginCluster = originCluster,
            RetryCluster = null
        };


        configBuilderMock.Setup(builder => builder.BuildConsumerConfig(It.Is<IDictionary<string, string>>(dict => dict.Equals(originCluster))))
            .Returns(() => consumerConfig);
        configBuilderMock.Setup(builder => builder.BuildProducerConfig(It.Is<IDictionary<string, string>>(dict => dict.Equals(originCluster))))
            .Returns(() => producerConfig);

        localConsumerBuilder.Setup(builder => builder.BuildConsumer(consumerConfig, producerConfig, _topicNames))
            .Returns(() => consumerMock.Object);

        var sut = new ConsumerFactory(localConsumerBuilder.Object, configBuilderMock.Object);

        sut.BuildRetryConsumer(config, _topicNames);
        mockFactory.Verify();
    }

    /// <summary>
    ///     Make sure the correct settings are used where retry is supplied
    /// </summary>
    [Fact]
    public void Use_Retry_Settings_For_Consumer_And_Retry_For_Consumer_If_Retry_Supplied() {
        var mockFactory = new MockRepository(MockBehavior.Strict);
        var localConsumerBuilder = mockFactory.Create<ILocalConsumerFactory>();
        var consumerMock = mockFactory.Create<IConsumer<byte[], byte[]>>();
        var configBuilderMock = mockFactory.Create<KafkaConfigBuilder>();
        var consumerConfig = new ConsumerConfig();
        var producerConfig = new ProducerConfig();

        var originCluster = new Dictionary<string, string> {
            ["group.id"] = "origin"
        };
        var retryCluster = new Dictionary<string, string> {
            ["group.id"] = "retry"
        };

        configBuilderMock.Setup(builder => builder.BuildConsumerConfig(It.Is<IDictionary<string, string>>(dict => dict.Equals(retryCluster))))
            .Returns(() => consumerConfig);
        configBuilderMock.Setup(builder => builder.BuildProducerConfig(It.Is<IDictionary<string, string>>(dict => dict.Equals(retryCluster))))
            .Returns(() => producerConfig);

        localConsumerBuilder.Setup(builder => builder.BuildConsumer(consumerConfig, producerConfig, _topicNames))
            .Returns(() => consumerMock.Object);

        var sut = new ConsumerFactory(localConsumerBuilder.Object, configBuilderMock.Object);

        var config = new KafkaRetryConfig {
            OriginCluster = originCluster,
            RetryCluster = retryCluster
        };

        sut.BuildRetryConsumer(config, _topicNames);
        mockFactory.Verify();
    }
}