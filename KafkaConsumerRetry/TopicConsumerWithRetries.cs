using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KafkaConsumerRetry
{
    public class TopicConsumerWithRetries<TKey, TValue> : IDisposable
    {
        private readonly IConsumerFactory<TKey, TValue> _consumerFactory;
        private readonly ISupportTopicNameGenerator _nameGenerator;
        private readonly ITopicManagement _topicManagement;

        public TopicConsumerWithRetries(IConsumerFactory<TKey, TValue> consumerFactory, ISupportTopicNameGenerator nameGenerator,
            ITopicManagement topicManagement)
        {
            _consumerFactory = consumerFactory;
            _nameGenerator = nameGenerator;
            _topicManagement = topicManagement;
        }

        public async Task MonitorSupportedTopic<T>(string topic, CancellationToken token)
        {
            // get topic names
            // ensure topics exist
            // subscribe to all topics
            TopicNaming topicNaming = _nameGenerator.GetTopicNaming(topic);
            await _topicManagement.EnsureTopicSettings(topicNaming);
            await _consumerFactory.StartConsumers(topic, DoStuff, token);
            
        }

        private Task DoStuff(byte[] arg1, byte[] arg2)
        {
            try
            {
                var key = await _keyDeserializer.DeserializeAsync(consume.Message.Key, false,
                    new SerializationContext(MessageComponentType.Key, consume.Topic, consume.Message.Headers));

                var value = await _valueDeserializer.DeserializeAsync(consume.Message.Value, false,
                    new SerializationContext(MessageComponentType.Value, consume.Topic, consume.Message.Headers));

                await handleDeserialized(key, value);
            }
            catch (Exception exception)
            {
                // identify this consumers retry group name
                AppendException(exception, retryGroupName, consume.Message);

                producer.Produce(GetNextTopic(consume.Topic, topicNaming), consume.Message);
            }
        }
    }
}