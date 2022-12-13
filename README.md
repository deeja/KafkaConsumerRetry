# UBER style retry architecture

## How it works

This library implements a retry system by using retry topics. 

The original article text is in the local [ARTICLE.md](./ARTICLE.md) or
on [Uber](https://eng.uber.com/reliable-reprocessing/)

### Main considerations
- Retry spamming
- Worker starvation
- Too many threads
  
### Retry Spamming 

### Worker Starvation 



## Usage 

```csharp

var retryServiceConfig = new KafkaRetryConfig {
        RetryAttempts = 3,
        RetryBaseTime = TimeSpan.FromSeconds(5),
        OriginCluster = new Dictionary<string, string> {
            ["group.id"] = "my-group-name",
            ["bootstrap.servers"] = "localhost:9092",
            ["client.id"] = "client-id",
            ["auto.offset.reset"] = "earliest",
            ["enable.auto.offset.store"] = "false", //Don't auto save the offset
            ["enable.auto.commit"] = "true" // Allow auto commit
        }
    };
var topicNaming = _naming.GetTopicNaming(originalName,retryServiceConfig);
await _consumerRunner.RunConsumersAsync<TestingResultHandler>(retryServiceConfig, topicNaming, cancellationToken);
```

## Deep dive

### Topic Naming
Naming of the topic 


| Class | Usage |
| ------|-------|
| KafkaRetryConfig |  Connection settings for the Origin and Retry Kafka Clusters. If no retry cluster is specified, then the origin will be used for the retry topics |
| TopicNaming | Responsible for the naming of the retries and dlq | 
| PartitionManager | Controls the actions of the workers. More info below | 
| 


### PartitionManager

`PartitionMessageManager` handles messages from the subscribed topics. 
Internal message queues are updated on `Assigned`, `Revoked` and `Lost` partition events. 

Incoming messages are added to a partition's work queue until a threshold is reached. When the threshold is reached, the `Pause()` action is called on that topic's partition. 

The `Pause()` call is not passed to the server, but is used by the internal Kafka library [`librdkafka`](https://github.com/edenhill/librdkafka) to stop the requesting of messages from that topic's partition. 

`Resume()` is called when the work queue reaches zero. 









