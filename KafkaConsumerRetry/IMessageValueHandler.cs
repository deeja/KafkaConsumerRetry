using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public interface IMessageValueHandler
    {
        Task HandleAsync(Message<byte[], byte[]> message, CancellationToken cancellationToken);
    }
}