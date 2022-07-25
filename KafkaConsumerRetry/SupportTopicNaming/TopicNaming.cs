using System.Collections.Generic;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.SupportTopicNaming {
    public class TopicNaming
        : ITopicNaming {
        private readonly RetryServiceConfig _config;

        public TopicNaming(RetryServiceConfig config) {
            _config = config;
        }

        public Configuration.TopicNaming GetTopicNaming(string topic) {
            var retries = new List<string>();
            for (var i = 0; i < _config.RetryAttempts; i++) {
                retries.Add($"{topic}.retry.{i}");
            }

            return new Configuration.TopicNaming(topic, retries.ToArray(), topic + ".dlq");
        }
    }
}