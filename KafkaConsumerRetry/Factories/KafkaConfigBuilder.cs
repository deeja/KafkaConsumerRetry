using Confluent.Kafka;

namespace KafkaConsumerRetry.Factories;

public class KafkaConfigBuilder : IKafkaConfigBuilder {
    public virtual ConsumerConfig BuildConsumerConfig(IDictionary<string, string> keyValues) {
        var consumerConfig = new ConsumerConfig(keyValues);
        SetClusterDefaults(consumerConfig);
        return consumerConfig;
    }

    public virtual ProducerConfig BuildProducerConfig(IDictionary<string, string> keyValues) {
        return new ProducerConfig(keyValues);
    }
    
    protected virtual void SetClusterDefaults(ConsumerConfig clusterSettings) {
        clusterSettings.AutoOffsetReset = AutoOffsetReset.Earliest; // Get the first available messages when setting up consumer group
        clusterSettings.EnableAutoOffsetStore = false; //Don't auto save the offset; this is done inside the error handling
        clusterSettings.EnableAutoCommit = true; // Allow auto commit
    }
}