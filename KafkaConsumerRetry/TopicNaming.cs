namespace KafkaConsumerRetry
{
    public class TopicNaming
    {
        /// <summary>
        /// Topic that was provided to generate the names of the supporting topics
        /// </summary>
        public string Origin { get; set; }
        /// <summary>
        /// Topics that are subscribed to; allows retries
        /// </summary>
        public string[] Retries { get; set; }
        /// <summary>
        /// The final destination for when retries fail
        /// </summary>
        public string DeadLetter { get; set; }
    }
}