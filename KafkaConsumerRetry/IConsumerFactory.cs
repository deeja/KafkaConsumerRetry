using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public interface IConsumerFactory<TKey, TValue>
    {
        Task StartConsumers(string topicName,
            Func<TKey, TValue, Task> handleDeserialized, CancellationToken token);
    }

    /// <summary>
    ///     Starts and subscribes to messages
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    internal class ConsumerThing<TKey, TValue> : IConsumerFactory<TKey, TValue>
    {
        private readonly RetryServiceConfig _config;
        private readonly IConsumeResultAllocator _consumeResultAllocator;
        private readonly ISupportTopicNameGenerator _generator;

        public ConsumerThing(RetryServiceConfig config, ISupportTopicNameGenerator generator,
            IConsumeResultAllocator consumeResultAllocator)
        {
            _config = config;
            _generator = generator;
            _consumeResultAllocator = consumeResultAllocator;
        }

        public virtual async Task StartConsumers(string topicName,
            Func<TKey, TValue, Task> handleDeserialized, CancellationToken token)
        {
            ConsumerBuilder<byte[], byte[]> builder = new(_config.TopicKafka);

            var topicConsumer = builder.Build();
            IConsumer<byte[], byte[]>? retryConsumer = null;

            // setup producer
            if (_config.RetryKafka is { } retryConfig)
            {
                retryConsumer = new ConsumerBuilder<byte[], byte[]>(retryConfig).Build();
            }

            var topicNaming = _generator.GetTopicNaming(topicName);
            if (retryConsumer is null)
            {
                // one consumer for all topics
                var mainAndRetries = new List<string> {topicNaming.Origin};
                mainAndRetries.AddRange(topicNaming.Retries);
                topicConsumer.Subscribe(mainAndRetries);
            }
            else
            {
                topicConsumer.Subscribe(topicNaming.Origin);
                retryConsumer.Subscribe(topicNaming.Retries);
            }

            // get the group id from the setting for the retry
            string retryGroupId = (_config.RetryKafka ?? _config.TopicKafka)["group.id"];

            List<Task> tasks = new()
                {ConsumeAsync(topicConsumer, topicNaming, token, retryGroupId)};
            if (retryConsumer is { })
            {
                tasks.Add(ConsumeAsync(retryConsumer, topicNaming, token, retryGroupId));
            }

            await Task.WhenAll(tasks);
        }

        private async Task ConsumeAsync(IConsumer<byte[], byte[]> consumer, TopicNaming topicNaming,
            CancellationToken token, string retryGroupId)
        {
            await Task.Yield();
            while (!token.IsCancellationRequested)
            {
                var consume = consumer.Consume(token);
                _consumeResultAllocator.AddConsumeResult(consume, consumer,token, retryGroupId, GetNextTopic(consume.Topic, topicNaming));
            }
        }

        private string GetNextTopic(string topic, TopicNaming topicNaming)
        {
            // if straight from the main topic, then use first retry
            if (topic == topicNaming.Origin)
            {
                return topicNaming.Retries.Any() ? topicNaming.Retries[0] : topicNaming.DeadLetter;
            }

            // if any of the retries except the last, then use the next
            for (var i = 0; i < topicNaming.Retries.Length - 1; i++)
                if (topicNaming.Retries[i] == topic)
                {
                    return topicNaming.Retries[i + 1];
                }

            // otherwise dlq
            return topicNaming.DeadLetter;
        }
    }
}