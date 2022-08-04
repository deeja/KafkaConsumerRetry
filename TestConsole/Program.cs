// See https://aka.ms/new-console-template for more information

using KafkaConsumerRetry;
using KafkaConsumerRetry.Configuration;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TestConsole;

IServiceCollection services = new ServiceCollection();
services.AddSingleton(new RetryServiceConfig {
    RetryAttempts = 3,
    MaxConcurrent = 100,
    RetryBaseTime = TimeSpan.FromSeconds(5),
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
services.AddLogging(builder => builder.AddSimpleConsole(options => {
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
}));
services.AddSingleton<Runner>()
    .AddSingleton<IConsumerResultHandler, WriteToLoggerConsumerResultHandler>();
var sp = services.BuildServiceProvider();
var requiredService = sp.GetRequiredService<Runner>();
var cancellationToken = CancellationToken.None;
await requiredService.ExecuteAsync(cancellationToken);