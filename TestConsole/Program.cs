using KafkaConsumerRetry;
using KafkaConsumerRetry.Handlers;
using KafkaConsumerRetry.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using TestConsole;

IServiceCollection services = new ServiceCollection();

services.AddKafkaConsumerRetry(30, TimeSpan.FromSeconds(2));
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
await requiredService.ExecuteAsync(cancellationToken);