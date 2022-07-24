// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Threading;
using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TestConsole;

IServiceCollection services = new ServiceCollection();
services.AddSingleton(new RetryServiceConfig {
    RetryAttempts = 3,
    TopicKafka = new Dictionary<string, string> {
        ["group.id"] = "my-group-name",
        ["bootstrap.servers"] = "localhost:9092",
        ["client.id"] = "client-id",
        ["enable.auto.commit"] = "false" // required
    }
});
services.AddKafkaConsumerRetry();
services.AddLogging();
services.AddSingleton<Runner>();
var sp = services.BuildServiceProvider();
var requiredService = sp.GetRequiredService<Runner>();
var cancellationToken = CancellationToken.None;
await requiredService.ExecuteAsync(cancellationToken);