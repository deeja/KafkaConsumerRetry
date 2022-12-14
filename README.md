# UBER style retry architecture
## Features:
- Multi cluster; retries can be on a different Kafka cluster. i.e. one that is in your control
- Timeouts and Retries are configurable
- Topic names are configurable
- Restrict number of "messages at a time"
- Partition "pausing" using the `librdkafka` API
- Gracefully handles allocation/loss of a consumer's partitions

## How it works

This library implements a retry system by using retry topics. 

The original article text is in the local [ARTICLE.md](./ARTICLE.md) or
on [Uber](https://eng.uber.com/reliable-reprocessing/)

### Main considerations that have been avoided by this library

#### Retry spamming

Retry spamming happens when a message is placed back into a queue when the time between attempts has not been reached.
This is a bad idea generally as it can flood messages, while also reducing visibility.

#### Worker starvation due to retry waits
In most implementations the consume is called and a message is returned.
This is then processed, and the cycle happens again.

With retries, often the thread is delayed until the message's retry time is up at which point the message is processed.
This has a few of problems.
- Not calling `Consume`  regularly will drop a consumer out of the consumer group. 
- While the processor is waiting to consume, processor time is wasted.

Sometimes this is mitigated by pushing the message back onto the retry queue, but this then becomes Retry Spamming.

#### Too many threads doing something at the same time
Sometimes it's not a good idea to do as much as possible. 
For example, when connecting to databases there is usually a limit to the number of connections that a process can have.

#### Avoiding main topic retries
Pushing messages to the main topic for retries is dirty. 
- Reduces visibility of system functions e.g. A dev asking "why is this message here multiple times?"
- You may not have the rights to push to the origin server topic
- Services that are not part of your retry system may be looking at that topic and process the message again


## Usage

This section might be out of date as this develops, so for the truth refer to the `TestConsole.csproj` 

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

| Class                   | Usage |
|-------------------------|-------|
| KafkaRetryConfig        |  Connection settings for the Origin and Retry Kafka Clusters. If no retry cluster is specified, then the origin will be used for the retry topics |
| TopicNames              | Responsible for the naming of the retries and dlq | 
| PartitionMessageManager | Controls the actions of the workers. More info below |

### PartitionMessageManager

`PartitionMessageManager` handles messages from the subscribed topics.
Internal message queues are updated on `Assigned`, `Revoked` and `Lost` partition events.

Incoming messages are added to a partition's work queue until a threshold is reached. When the threshold is reached,
the `Pause()` action is called on that topic's partition.

The `Pause()` call is not passed to the server, but is used by the internal Kafka
library [`librdkafka`](https://github.com/edenhill/librdkafka) to stop the requesting of messages from that topic's
partition.

`Resume()` is called when the work queue reaches zero. 









