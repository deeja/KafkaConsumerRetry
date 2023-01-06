using Confluent.Kafka;

namespace KafkaConsumerRetry;

/// <summary>
/// Decoration based adapter for IConsumerBuilder
/// </summary>
public class LocalConsumerBuilder : IConsumerBuilder {
    private readonly ConsumerBuilder<byte[], byte[]> _consumerBuilder;

    public LocalConsumerBuilder(ConsumerBuilder<byte[], byte[]> consumerBuilder) {
        _consumerBuilder = consumerBuilder;
    }

    public IConsumerBuilder SetStatisticsHandler(Action<IConsumer<byte[], byte[]>, string> statisticsHandler) {
        _consumerBuilder.SetStatisticsHandler(statisticsHandler);
        return this;
    }

    public IConsumerBuilder SetErrorHandler(Action<IConsumer<byte[], byte[]>, Error> errorHandler) {
        _consumerBuilder.SetErrorHandler(errorHandler);
        return this;
    }

    public IConsumerBuilder SetLogHandler(Action<IConsumer<byte[], byte[]>, LogMessage> logHandler) {
        _consumerBuilder.SetLogHandler(logHandler);
        return this;
    }

    public IConsumerBuilder SetOAuthBearerTokenRefreshHandler(Action<IConsumer<byte[], byte[]>, string> oAuthBearerTokenRefreshHandler) {
        _consumerBuilder.SetOAuthBearerTokenRefreshHandler(oAuthBearerTokenRefreshHandler);
        return this;
    }

    public IConsumerBuilder SetKeyDeserializer(IDeserializer<byte[]> deserializer) {
        _consumerBuilder.SetKeyDeserializer(deserializer);
        return this;
    }

    public IConsumerBuilder SetValueDeserializer(IDeserializer<byte[]> deserializer) {
        _consumerBuilder.SetValueDeserializer(deserializer);
        return this;
    }

    public IConsumerBuilder SetPartitionsAssignedHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionsAssignedHandler) {
        _consumerBuilder.SetPartitionsAssignedHandler(partitionsAssignedHandler);
        return this;
    }

    public IConsumerBuilder SetPartitionsAssignedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartition>> partitionAssignmentHandler) {
        _consumerBuilder.SetPartitionsAssignedHandler(partitionAssignmentHandler);
        return this;
    }

    public IConsumerBuilder SetPartitionsRevokedHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsRevokedHandler) {
        _consumerBuilder.SetPartitionsRevokedHandler(partitionsRevokedHandler);
        return this;
    }

    public IConsumerBuilder SetPartitionsRevokedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsRevokedHandler) {
        _consumerBuilder.SetPartitionsRevokedHandler(partitionsRevokedHandler);
        return this;
    }

    public IConsumerBuilder SetPartitionsLostHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsLostHandler) {
        _consumerBuilder.SetPartitionsLostHandler(partitionsLostHandler);
        return this;
    }

    public IConsumerBuilder SetPartitionsLostHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsLostHandler) {
        _consumerBuilder.SetPartitionsLostHandler(partitionsLostHandler);
        return this;
    }

    public IConsumerBuilder SetOffsetsCommittedHandler(Action<IConsumer<byte[], byte[]>, CommittedOffsets> offsetsCommittedHandler) {
        _consumerBuilder.SetOffsetsCommittedHandler(offsetsCommittedHandler);
        return this;
    }

    public IConsumer<byte[], byte[]> Build() {
        return _consumerBuilder.Build();
    }
}