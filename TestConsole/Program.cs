using KafkaConsumerRetry;
using KafkaConsumerRetry.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TestConsole;

IServiceCollection services = new ServiceCollection();

// Time used by DelayCalculator
var delayBase = TimeSpan.Zero;
// Messages that can be processed at a time
var maximumConcurrentTasks = 10;
// Test messages to create
var messageCount = 1000;
// Partitions per topic created
var partitionsPerTopic = 12;
// Reattempt times
var retryAttempts = 1;

// Topic count = Origin + DLQ + Retries


services.AddKafkaConsumerRetry(maximumConcurrentTasks, delayBase);
services.AddLogging(builder => builder.AddSimpleConsole(options => {
    options.ColorBehavior = LoggerColorBehavior.Default;
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
}));
services.AddSingleton<Runner>()
    .AddSingleton<IConsumerResultHandler, TestingResultHandler>();
var sp = services.BuildServiceProvider();
var runner = sp.GetRequiredService<Runner>();
var cancellationToken = CancellationToken.None;

await runner.ExecuteAsync(messageCount, cancellationToken, partitionsPerTopic, retryAttempts);