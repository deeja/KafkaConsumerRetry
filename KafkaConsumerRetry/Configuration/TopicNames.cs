namespace KafkaConsumerRetry.Configuration;

public record TopicNames(string Origin, string[] Retries, string DeadLetter);