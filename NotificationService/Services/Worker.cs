using NotificationService.Models;

namespace NotificationService.Services
{
    public class Worker(ILogger<Worker> logger, IKafkaConsumer kafkaConsumer, TopicConfiguration topicConfiguration) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Worker service starting");

            try
            {
                await kafkaConsumer.ConsumeAsync(topicConfiguration.Topics, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Worker service stopped");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker service encountered an error");
                throw;
            }
        }
    }
}
