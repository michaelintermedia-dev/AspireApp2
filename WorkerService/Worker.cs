using Confluent.Kafka;

namespace WorkerService;

public class Worker(ILogger<Worker> logger, IConsumer<string, string> kafkaConsumer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string topic = "weather-forecast-events";
        
        try
        {
            kafkaConsumer.Subscribe(topic);
            logger.LogInformation("Worker subscribed to Kafka topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = kafkaConsumer.Consume(TimeSpan.FromSeconds(1));
                    
                    if (consumeResult != null)
                    {
                        logger.LogInformation(
                            "Received message from Kafka - Key: {Key}, Value: {Value}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key,
                            consumeResult.Message.Value,
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value);
                    }
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Error consuming message from Kafka");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error in Kafka consumer");
        }
        finally
        {
            kafkaConsumer.Close();
            logger.LogInformation("Kafka consumer closed");
        }
    }
}
