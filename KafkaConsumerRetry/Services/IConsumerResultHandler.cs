using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry.Services {
    /// <summary>
    ///     For processing the incoming messages after the have been allocated to run
    /// </summary>
    public interface IConsumerResultHandler {
        Task HandleAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
    }
}