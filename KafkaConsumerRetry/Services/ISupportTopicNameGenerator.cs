using System.Collections.Generic;
using KafkaConsumerRetry.Configuration;

namespace KafkaConsumerRetry.Services {
    public interface ISupportTopicNameGenerator {
        TopicNaming GetTopicNaming(string topic);
    }

    public class SupportTopicNameGenerator
        : ISupportTopicNameGenerator {
        private readonly RetryServiceConfig _config;

        public SupportTopicNameGenerator(RetryServiceConfig config) {
            _config = config;
        }

        public TopicNaming GetTopicNaming(string topic) {
            var retries = new List<string>();
            for (var i = 0; i < _config.RetryAttempts; i++) {
                retries.Add($"{topic}.retry.{i}");
            }

            return new TopicNaming(topic, retries.ToArray(), topic + ".dlq");
        }
    }
}