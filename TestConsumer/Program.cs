// See https://aka.ms/new-console-template for more information

using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TestConsole;

if (!int.TryParse(Environment.GetEnvironmentVariable("START_AFTER"), out var startAfterSeconds)) {
    startAfterSeconds = 0;
}

if (!int.TryParse(Environment.GetEnvironmentVariable("STOP_AFTER"), out var stopAfterSeconds)) {
    stopAfterSeconds = -1;
}

IServiceCollection services = new ServiceCollection();
var bootstrap = Environment.GetEnvironmentVariable("BOOTSTRAP") ?? "localhost:9092";
var groupId = Environment.GetEnvironmentVariable("GROUP_ID") ?? "my-test-group";
var clientId = Environment.GetEnvironmentVariable("CLIENT_ID") ??"client-id";
var topic = Environment.GetEnvironmentVariable("TOPIC") ??"test";

services.AddSingleton(new RetryServiceConfig {
    RetryAttempts = 3,
    MaxConcurrent = 100,
    RetryBaseTime = TimeSpan.FromSeconds(5),
    TopicKafka = new Dictionary<string, string> {
        ["group.id"] = groupId,
        ["bootstrap.servers"] = bootstrap,
        ["client.id"] = clientId,
        ["auto.offset.reset"] = "earliest",
        ["enable.auto.offset.store"] = "false", //TODO: Force Don't auto save the offset
        ["enable.auto.commit"] = "true" // TODO: Force set auto commit
    }
});
services.AddKafkaConsumerRetry();
services.AddLogging(builder => builder.AddSimpleConsole(options => {
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
}));
services.AddSingleton<Runner>()
    .AddSingleton<IConsumerResultHandler, TestingResultHandler>();
var sp = services.BuildServiceProvider();
var requiredService = sp.GetRequiredService<Runner>();
var cancellationToken = CancellationToken.None;
await requiredService.ExecuteAsync(startAfterSeconds, stopAfterSeconds, topic, cancellationToken);