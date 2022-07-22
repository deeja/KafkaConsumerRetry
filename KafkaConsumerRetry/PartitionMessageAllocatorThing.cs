using System.Collections.Generic;
using System.Threading;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public class PartitionConsumeResultAllocatorThing : IConsumeResultAllocator
    {
        private readonly IMessageValueHandler _handler;
        private readonly Dictionary<TopicPartition, TopicPartitionQueue> _partitionQueues = new();
        private readonly IProducer<byte[], byte[]> _producer;

        public PartitionConsumeResultAllocatorThing(IMessageValueHandler handler, IProducer<byte[], byte[]> producer)
        {
            _handler = handler;
            _producer = producer;
        }

        public void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult, IConsumer<byte[], byte[]> consumer,
            CancellationToken cancellationToken, string retryGroupId, string nextTopic)
        {
            var topicPartition = consumeResult.TopicPartition;
            if (!_partitionQueues.ContainsKey(topicPartition))
            {
                _partitionQueues.Add(topicPartition, new TopicPartitionQueue(_handler, cancellationToken, consumer, topicPartition, _producer, retryGroupId, nextTopic));
            }

            var partitionQueue = _partitionQueues[topicPartition];

            partitionQueue.AddAllocation(consumeResult);
        }
    }

    public interface IConsumeResultAllocator
    {
        void AddConsumeResult(ConsumeResult<byte[], byte[]> consumeResult, IConsumer<byte[], byte[]> consumer,
            CancellationToken cancellationToken, string retryGroupId, string nextTopic);
    }
}