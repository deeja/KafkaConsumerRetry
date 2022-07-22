using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public class TopicConsumerWithRetries {
        private readonly IConsumerFactory _consumerFactory;
        private readonly ISupportTopicNameGenerator _nameGenerator;
        private readonly ITopicManager _topicManager;

        public TopicConsumerWithRetries(IConsumerFactory consumerFactory, ISupportTopicNameGenerator nameGenerator,
            ITopicManager topicManager) {
            _consumerFactory = consumerFactory;
            _nameGenerator = nameGenerator;
            _topicManager = topicManager;
        }

        public async Task MonitorSupportedTopic(string topic, CancellationToken token) {
            // get topic names
            // ensure topics exist
            // subscribe to all topics
            TopicNaming topicNaming = _nameGenerator.GetTopicNaming(topic);
            await _topicManager.EnsureTopicSettings(topicNaming);
            await _consumerFactory.StartConsumers(topic, token);
        }
    }
}