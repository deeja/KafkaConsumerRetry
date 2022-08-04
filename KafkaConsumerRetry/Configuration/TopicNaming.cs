namespace KafkaConsumerRetry.Configuration;

public record TopicNaming {
    public TopicNaming(string origin, string[] retries, string deadLetter) {
        Origin = origin;
        Retries = retries;
        DeadLetter = deadLetter;
    }

    /// <summary>
    ///     Topic that was provided to generate the names of the supporting topics
    /// </summary>
    public string Origin { get; }

    /// <summary>
    ///     Topics that are subscribed to; allows retries
    /// </summary>
    public string[] Retries { get; }

    /// <summary>
    ///     The final destination for when retries fail
    /// </summary>
    public string DeadLetter { get; }
}