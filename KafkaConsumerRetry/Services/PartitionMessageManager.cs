using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public sealed class PartitionMessageManager : PartitionMessageManagerBase {

    public PartitionMessageManager(IPartitionProcessorFactory partitionProcessorFactory, ILogger<PartitionMessageManager> logger, IServiceProvider serviceProvider) :
        base(partitionProcessorFactory, logger, serviceProvider) { }
}