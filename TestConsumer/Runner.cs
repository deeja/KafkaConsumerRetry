using KafkaConsumerRetry.Services;
using KafkaConsumerRetry.SupportTopicNaming;
using Microsoft.Extensions.Logging;

namespace TestConsole;

public class Runner {
    private readonly IConsumerRunner _consumerRunner;
    private readonly ITopicNameGenerator _nameGenerator;
    private readonly ILogger<Runner> _logger;

    public Runner(IConsumerRunner consumerRunner, ITopicNameGenerator nameGenerator, ILogger<Runner> logger) {
        _consumerRunner = consumerRunner;
        _nameGenerator = nameGenerator;
        _logger = logger;
    }

    public async Task ExecuteAsync(int startAfterSeconds, int stopAfterSeconds, string topic, CancellationToken cancellationToken) {
        
        var topicNaming = _nameGenerator.GetTopicNaming(topic);

        var startDelay = TimeSpan.FromSeconds(startAfterSeconds);
        if (startDelay > TimeSpan.Zero) {
            _logger.LogInformation("Start Delay set to {Delay}. This application will start it's consumers at {Time}", startAfterSeconds, DateTime.Now + startDelay);
        }

        var stopDelay = TimeSpan.FromSeconds(stopAfterSeconds);
        if (stopDelay > TimeSpan.Zero) {
            _logger.LogInformation("Stop Delay has been set {Delay}. This application will exit at {Time}", stopDelay, DateTime.Now + stopDelay);
            Func<Task> _ = async () => {
                await Task.Delay(stopDelay, cancellationToken);
                _logger.LogInformation("Exiting application due to Stop Delay of {Delay}", stopDelay);
                Environment.Exit(0);
            };
        }

        await Task.Delay(startDelay, cancellationToken);
        await _consumerRunner.RunConsumersAsync(topicNaming, cancellationToken);
    }


   
}