using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public class TopicConsumerWithRetries {
        private readonly IConsumerFactory _consumerFactory;
        private readonly ISupportTopicNameGenerator _nameGenerator;
        private readonly ITopicManagement _topicManagement;

        public TopicConsumerWithRetries(IConsumerFactory consumerFactory, ISupportTopicNameGenerator nameGenerator,
            ITopicManagement topicManagement) {
            _consumerFactory = consumerFactory;
            _nameGenerator = nameGenerator;
            _topicManagement = topicManagement;
        }

        public async Task MonitorSupportedTopic(string topic, CancellationToken token) {
            // get topic names
            // ensure topics exist
            // subscribe to all topics
            TopicNaming topicNaming = _nameGenerator.GetTopicNaming(topic);
            await _topicManagement.EnsureTopicSettings(topicNaming);
            await _consumerFactory.StartConsumers(topic, token);
        }
    }
}