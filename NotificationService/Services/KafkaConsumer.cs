using Confluent.Kafka;

namespace NotificationService.Services
{
    public interface IKafkaConsumer
    {
        Task ConsumeAsync(string[] topics, CancellationToken cancellationToken);
    }

    public class KafkaConsumer(
        ILogger<KafkaConsumer> logger,
        IConsumer<string, string> consumer,
        IMessageDispatcher messageDispatcher) : IKafkaConsumer
    {
        public async Task ConsumeAsync(string[] topics, CancellationToken cancellationToken)
        {
            try
            {
                consumer.Subscribe(topics);
                logger.LogInformation("Subscribed to Kafka topics: {Topics}", string.Join(", ", topics));

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(cancellationToken);

                        if (consumeResult.IsPartitionEOF)
                            continue;

                        logger.LogInformation(
                            "Received message from {Topic}[{Partition}] at offset {Offset}: {Message}",
                            consumeResult.Topic,
                            consumeResult.Partition,
                            consumeResult.Offset,
                            consumeResult.Message.Value);

                        await messageDispatcher.DispatchAsync(consumeResult.Topic, consumeResult.Message.Value, cancellationToken);
                    }
                    catch (ConsumeException ex)
                    {
                        logger.LogError(ex, "Error consuming message from Kafka");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Kafka consumer stopping");
            }
            finally
            {
                consumer.Close();
            }
        }
    }
}
