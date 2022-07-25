using System;
using System.Linq;
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
        private readonly RetryServiceConfig _config;
        private readonly IConsumerFactory _consumerFactory;
        private readonly ITopicPartitionQueueManager _queueManager;

        public ReliableRetryRunner(RetryServiceConfig config, IConsumerFactory consumerFactory,
            ITopicPartitionQueueManager queueManager) {
            _config = config;
            _consumerFactory = consumerFactory;
            _queueManager = queueManager;
        }

        public virtual async Task RunConsumersAsync(TopicNaming topicNaming, CancellationToken token) {
            var originConsumer = _consumerFactory.BuildOriginConsumer();
            var retryConsumer = _consumerFactory.BuildRetryConsumer();

            originConsumer.Subscribe(topicNaming.Origin);
            retryConsumer.Subscribe(topicNaming.Retries);

            // get the group id from the setting for the retry -- need a better way of doing this
            string retryGroupId = (_config.RetryKafka ?? _config.TopicKafka)["group.id"];

            await Task.WhenAll(ConsumeAsync(originConsumer, topicNaming, token, retryGroupId),
                ConsumeAsync(retryConsumer, topicNaming, token, retryGroupId));
        }

        private async Task ConsumeAsync(IConsumer<byte[], byte[]> consumer, TopicNaming topicNaming,
            CancellationToken cancellationToken, string retryGroupId) {
            await Task.Yield();
            while (!cancellationToken.IsCancellationRequested) {
                var consumeResult = consumer.Consume(cancellationToken);

                // TODO: this is not great. shouldn't return the current index and next topic
                var currentIndexAndNextTopic = GetCurrentIndexAndNextTopic(consumeResult.Topic, topicNaming);
                _queueManager.AddConsumeResult(consumeResult, consumer, retryGroupId,
                    currentIndexAndNextTopic.NextTopic, currentIndexAndNextTopic.CurrentIndex);
            }
        }

        /// <summary>
        ///     Gets the current index of the topic, and also the next topic to push to
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="topicNaming"></param>
        /// <returns></returns>
        private (int CurrentIndex, string NextTopic)
            GetCurrentIndexAndNextTopic(string topic, TopicNaming topicNaming) {
            // if straight from the main topic, then use first retry
            if (topic == topicNaming.Origin) {
                return (0, topicNaming.Retries.Any() ? topicNaming.Retries[0] : topicNaming.DeadLetter);
            }

            // if any of the retries except the last, then use the next
            for (var i = 0; i < topicNaming.Retries.Length - 1; i++) {
                if (topicNaming.Retries[i] == topic) {
                    return (i, topicNaming.Retries[i + 1]);
                }
            }

            // otherwise dlq -- must have at least one 
            return (Math.Max(1, topicNaming.Retries.Length), topicNaming.DeadLetter);
        }
    }
}