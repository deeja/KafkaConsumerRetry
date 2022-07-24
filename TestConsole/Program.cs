// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Threading;
using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestConsole;

IServiceCollection services = new ServiceCollection();
services.AddSingleton(new RetryServiceConfig {
    RetryAttempts = 3,
    TopicKafka = new Dictionary<string, string> {
        ["group.id"] = "my-group-name",
        ["bootstrap.servers"] = "localhost:9092",
        ["client.id"] = "client-id",
        ["auto.offset.reset"] = "earliest",
        ["enable.auto.offset.store"] = "false", //Don't auto save the offset
        ["enable.auto.commit"] = "true" // Allow auto commit
    }
});
services.AddKafkaConsumerRetry();
services.AddLogging(builder => builder.AddConsole());
services.AddSingleton<Runner>();
var sp = services.BuildServiceProvider();
var requiredService = sp.GetRequiredService<Runner>();
var cancellationToken = CancellationToken.None;

await requiredService.ExecuteAsync(cancellationToken);