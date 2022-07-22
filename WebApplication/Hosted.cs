using System.Threading;
using System.Threading.Tasks;
using KafkaConsumerRetry;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.Hosting;

namespace WebApplication {
    public class Hosted: BackgroundService {
        private readonly TopicConsumerWithRetries _consumerWithRetries;
        public Hosted(TopicConsumerWithRetries consumerWithRetries) {
            _consumerWithRetries = consumerWithRetries;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            await _consumerWithRetries.MonitorSupportedTopic("my-topic", stoppingToken);
        }
    }
}