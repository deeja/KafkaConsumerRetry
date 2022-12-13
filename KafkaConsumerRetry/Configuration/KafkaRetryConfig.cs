using System.ComponentModel.DataAnnotations;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
#pragma warning disable CS8618

namespace KafkaConsumerRetry.Configuration;

public class KafkaRetryConfig {
    /// <summary>
    ///     Times that a retry will be attempted before placing in DLQ
    /// </summary>
    [Required]
    public int RetryAttempts { get; set; }

    /// <summary>
    ///     Server settings where the original topic is located
    /// </summary>
    [Required]
    public IDictionary<string, string> OriginCluster { get; set; }

    /// <summary>
    ///     Server settings for the retries. If empty, the service will default to using <see cref="OriginCluster" />
    ///     Properties should match those found in <see cref="Confluent.Kafka.ProducerConfig" /> and
    ///     <seealso cref="Confluent.Kafka.ConsumerConfig" />
    /// </summary>
    public IDictionary<string, string>? RetryCluster { get; set; }
}