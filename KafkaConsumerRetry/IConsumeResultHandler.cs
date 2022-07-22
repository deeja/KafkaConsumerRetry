using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public interface IConsumeResultHandler<TKey, TValue>
    {
        Task HandleConsumeAsync(IConsumer<TKey, TValue> consumer,
            TopicNaming topicNaming,
            ConsumeResult<TKey, TValue> consumeResult,
            CancellationToken cancellationToken);
    }
}