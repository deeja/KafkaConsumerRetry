using KafkaConsumerRetry.Factories;
using Microsoft.Extensions.Logging;

namespace KafkaConsumerRetry.Services;

public sealed class PartitionMessageManager : PartitionMessageManagerBase {
    public PartitionMessageManager(IPartitionProcessorFactory partitionProcessorFactory, ILogger<PartitionMessageManager> logger, IServiceProvider serviceProvider,
        IProducerFactory producerFactory) : base(partitionProcessorFactory, logger, serviceProvider, producerFactory) { }
}