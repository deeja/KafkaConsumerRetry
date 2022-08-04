using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Factories;

namespace KafkaConsumerRetry.Services {
    /// <summary>
    ///     Starts and subscribes to messages
    /// </summary>
    public class ReliableRetryRunner : IReliableRetryRunner {
        private readonly IConsumerFactory _consumerFactory;
        private readonly ITopicPartitionQueueManager _queueManager;

        public ReliableRetryRunner(IConsumerFactory consumerFactory,
            ITopicPartitionQueueManager queueManager) {
            _consumerFactory = consumerFactory;
            _queueManager = queueManager;
        }

        public virtual async Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token) {
            var originConsumer = _consumerFactory.BuildOriginConsumer(topicNaming);
            var retryConsumer = _consumerFactory.BuildRetryConsumer(topicNaming);

            originConsumer.Subscribe(topicNaming.Origin);
            
            retryConsumer.Subscribe(topicNaming.Retries);

            await Task.WhenAll(ConsumeAsync(originConsumer, token),
                ConsumeAsync(retryConsumer, token));
        }

        private async Task ConsumeAsync(IConsumer<byte[], byte[]> consumer,
            CancellationToken cancellationToken) {
            await Task.Yield();
            while (!cancellationToken.IsCancellationRequested) {
                
                var consumeResult = consumer.Consume(cancellationToken);

                _queueManager.AddConsumeResult(consumeResult);
            }
        }

     
    }
}