using Confluent.Kafka;

namespace KafkaConsumerRetry;

/// <summary>
/// Confluent Kafka doesn't have an interface on their ConsumerBuilder, so using our own one. Will support all functions.
/// </summary>
public interface IConsumerBuilder {
    IConsumerBuilder SetStatisticsHandler(Action<IConsumer<byte[], byte[]>, string> statisticsHandler);
    IConsumerBuilder SetErrorHandler(Action<IConsumer<byte[], byte[]>, Error> errorHandler);
    IConsumerBuilder SetLogHandler(Action<IConsumer<byte[], byte[]>, LogMessage> logHandler);
    IConsumerBuilder SetOAuthBearerTokenRefreshHandler(Action<IConsumer<byte[], byte[]>, string> oAuthBearerTokenRefreshHandler);
    IConsumerBuilder SetKeyDeserializer(IDeserializer<byte[]> deserializer);
    IConsumerBuilder SetValueDeserializer(IDeserializer<byte[]> deserializer);

    IConsumerBuilder SetPartitionsAssignedHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartition>, IEnumerable<TopicPartitionOffset>> partitionsAssignedHandler);

    IConsumerBuilder SetPartitionsAssignedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartition>> partitionAssignmentHandler);

    IConsumerBuilder SetPartitionsRevokedHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsRevokedHandler);

    IConsumerBuilder SetPartitionsRevokedHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsRevokedHandler);

    IConsumerBuilder SetPartitionsLostHandler(
        Func<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>, IEnumerable<TopicPartitionOffset>> partitionsLostHandler);

    IConsumerBuilder SetPartitionsLostHandler(Action<IConsumer<byte[], byte[]>, List<TopicPartitionOffset>> partitionsLostHandler);
    IConsumerBuilder SetOffsetsCommittedHandler(Action<IConsumer<byte[], byte[]>, CommittedOffsets> offsetsCommittedHandler);
    IConsumer<byte[], byte[]> Build();
}