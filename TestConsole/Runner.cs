using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;

namespace TestConsole {
    public class Runner {
        private readonly IConsumerFactory _consumerFactory;
        private readonly ISupportTopicNameGenerator _nameGenerator;

        public Runner(IConsumerFactory consumerFactory, ISupportTopicNameGenerator nameGenerator) {
            _consumerFactory = consumerFactory;
            _nameGenerator = nameGenerator;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken) {
            string originalName = "my.topic";
            TopicNaming topicNaming = _nameGenerator.GetTopicNaming(originalName);
            await _consumerFactory.RunConsumersAsync(topicNaming, cancellationToken);
        }
    }
}