using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;

namespace KafkaConsumerRetry
{
    public class ConsumeResultHandler<TKey, TValue>:IConsumeResultHandler<TKey, TValue> {
        private readonly IMessageValueHandler<TValue> _messageValueHandler;
        private readonly IDelayCalculator<TKey, TValue> _delayCalculator;

        public ConsumeResultHandler(IMessageValueHandler<TValue> messageValueHandler, IDelayCalculator<TKey, TValue> delayCalculator )
        {
            _messageValueHandler = messageValueHandler;
            _delayCalculator = delayCalculator;
        }

        public async Task HandleConsumeAsync(IConsumer<TKey, TValue> consumer, TopicNaming topicNaming,
            ConsumeResult<TKey, TValue> consumeResult, CancellationToken cancellationToken)
        {
            // which retry queue is this in?
            // where should it go if it fails?
            // should the message be delayed?
            var messageValue = consumeResult.Message.Value;
            
            // block continue by topic partition
            
            // delay if needed
            var delay = _delayCalculator.Calculate(consumeResult, topicNaming);

            if (delay == TimeSpan.Zero)
            {
                var topicPartitions = new []{consumeResult.TopicPartition};
                consumer.Pause(topicPartitions);
                await Task.Delay(delay, cancellationToken);
                consumer.Resume(topicPartitions);
            }
            
            if (messageValue is { })
            {
                await _messageValueHandler.HandleAsync(TODO, messageValue, cancellationToken);
            }
        }
    }

    public interface IDelayCalculator<TKey, TValue>
    {
        TimeSpan Calculate(ConsumeResult<TKey,TValue> consumeResult, TopicNaming topicNaming);
    }
}