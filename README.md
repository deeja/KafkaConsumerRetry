 [![image](https://img.shields.io/nuget/v/KafkaConsumerRetry.svg?style=flat-square&label=nuget)](https://www.nuget.org/packages/KafkaConsumerRetry/)

# UBER style retry architecture
 - .Net Kafka Retry framework
 - Uses [Confluent's Kafka for .Net](https://github.com/confluentinc/confluent-kafka-dotnet)
## Features:
- Multi cluster; retries can be on a different Kafka cluster. i.e. one that is in your control
- Timeouts and Retries are configurable
- Topic names are configurable
- Restrict number of "messages at a time"
- Partition "pausing" using the `librdkafka` API
- Gracefully handles allocation/loss of a consumer's partitions

## How do I use it?

Check out the [ExampleProject](./ExampleProject/ExampleProject.csproj) in the root directory 

## Does it support deserialisation?
Yes. Take a look at the example project's handler.  

## Why use a retry?
Sometimes things don't work the first time, and that's ok.
A good example is when two events at the same time tell a processor to either update or create an entity in a database.
```mermaid
sequenceDiagram
    participant A1 as Subscriber One
    participant DB as Database
    participant A2 as Subscriber Two
    A1->>DB: Have you got X?
    DB->>A1: No
    A2->>DB: Have you got X?
    DB->>A2: No
    A1->>DB: Insert X
    DB->>A1: Done
    A2->>DB: Insert X
    DB->>A2: Failure!
    note over A2: Delay Execution before retry
    A2->>DB: Have you got X?
    DB->>A2: Yes, here you go.
    note over A2: Check that X should be updated
    A2->>DB: Update X
    DB->>A2: Ok
```

Embracing the retry means not being too concerned about timing issues 

## The Happy/Sad Path of a failing message
```mermaid
sequenceDiagram
    participant OT as Origin Topic
    participant CG as Consumer Group
    participant RT as Retry Topic 1
    participant RT2 as Retry Topic 2 
    participant DL as Dead Letter Topic 
    OT->>CG: Receive Message
    note over CG: "Exception!"
    CG->>RT: Push failed message
    RT->>CG: Receive failed message
    note over CG: Processing delayed until retry delay time
    note over CG: "Exception!"
    CG->>RT2: Push failed message for 2nd time
    RT2->>CG: Receive failed message
    note over CG: Processing delayed until retry delay time
    note over CG: "Exception!"
    CG->>DL: Push message to DLQ
```
### Notes
- Embrace the retry and don't be too concerned if messages end up in the first retry topic.
- If messages end up in the second or later topics then there might be an issue that needs looking at.
- DLQ is the end of the line, so make sure it's monitored!
- Retry times and retry delay are configurable per *consumer*
- Messages are queued for processing per partition, therefore keeping ordering where needed. 
- Delayed retries do not block other messages from being processed as they are handled per partition and are outside of the rate limiter
- In the case of retries, the messages on the same partition will have been added after the current message and so will need to wait *at least* the time the current message is waiting.
- The limit of parallel processing is restricted by the configurable `IRateLimiter`
- Exception information is carried along with the message that has failed. This can be viewed in the headers.
- Message exception failures are retried per *consumer group*; the consumer group information is stored in the headers of the pushed messages.
- If there are multiple consumer groups using the same retry topics, and more than one of those consumer groups fails, multiple copies of the message that failed will appear in the retry queue. This is normal and not a bug. 


The original article text is in the local [ARTICLE.md](ARTICLE.md) or
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









