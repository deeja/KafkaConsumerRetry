using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Hosting;

namespace WebApplication {
    public class Hosted: BackgroundService {
        private readonly IConsumerFactory _consumerFactory;
        private readonly ISupportTopicNameGenerator _nameGenerator;

        public Hosted(IConsumerFactory consumerFactory, ISupportTopicNameGenerator nameGenerator) {
            _consumerFactory = consumerFactory;
            _nameGenerator = nameGenerator;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
            string originalName = "my.topic";
            TopicNaming topicNaming = _nameGenerator.GetTopicNaming(originalName);
            await _consumerFactory.RunConsumersAsync(topicNaming, cancellationToken);
        }
    }
}