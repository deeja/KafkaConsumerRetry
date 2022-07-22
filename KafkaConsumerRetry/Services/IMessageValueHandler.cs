using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services
{
    public interface IMessageValueHandler
    {
        Task HandleAsync(Message<byte[], byte[]> message, CancellationToken cancellationToken);
    }
}