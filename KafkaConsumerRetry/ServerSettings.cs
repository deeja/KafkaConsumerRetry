using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public class RetryServiceConfig
    {
        /// <summary>
        /// Times that a retry will be attempted before placing in DLQ
        /// </summary>
        [Required]
        public int RetryAttempts { get; set; }
        /// <summary>
        /// Timespan before retry
        /// </summary>
        /// <remarks> [d'.']hh':'mm':'ss['.'fffffff]. https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings</remarks>
        [Required]
        public TimeSpan RetryBaseTime { get; set; }
        /// <summary>
        /// Server settings where the original topic is located
        /// </summary>
        [Required]
        public IDictionary<string, string> TopicKafka { get; set; }
        /// <summary>
        /// Server settings for the retries. If empty, the service will default to using <see cref="TopicKafka"/>
        /// Properties should match those found in <see cref="Confluent.Kafka.ProducerConfig"/> and <seealso cref="Confluent.Kafka.ConsumerConfig"/>
        /// </summary>
        public IDictionary<string, string>? RetryKafka { get; set; }
    }
}