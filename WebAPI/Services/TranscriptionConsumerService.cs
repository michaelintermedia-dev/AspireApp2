using System.Text.Json;
using Confluent.Kafka;

namespace WebAPI.Services;

public class TranscriptionConsumerService(
    ILogger<TranscriptionConsumerService> logger,
    IConsumer<string, string> kafkaConsumer,
    IServiceProvider serviceProvider) : BackgroundService
{
    private const string Topic = "audio.analyze.completed";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            kafkaConsumer.Subscribe(Topic);
            logger.LogInformation("Subscribed to Kafka topic: {Topic}", Topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = kafkaConsumer.Consume(stoppingToken);

                    if (consumeResult.IsPartitionEOF)
                    {
                        logger.LogDebug("Reached end of partition: {Partition} at offset {Offset}",
                            consumeResult.Partition, consumeResult.Offset);
                        continue;
                    }

                    logger.LogDebug("Message received from topic {Topic} partition {Partition} at offset {Offset}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset);

                    var notification = JsonSerializer.Deserialize<TranscriptionNotification>(
                        consumeResult.Message.Value,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (notification is null)
                    {
                        logger.LogWarning("Failed to deserialize transcription notification");
                        continue;
                    }

                    using var scope = serviceProvider.CreateScope();
                    var dbService = scope.ServiceProvider.GetRequiredService<IDbService>();
                    await dbService.SaveTranscriptionAsync(
                        notification.FileName,
                        notification.Status,
                        notification.ProcessedAt,
                        notification.TranscriptionData);

                    logger.LogInformation("Transcription saved for file: {FileName} with status: {Status}",
                        notification.FileName, notification.Status);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing transcription message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Transcription consumer stopping");
        }
        finally
        {
            kafkaConsumer.Close();
        }
    }
}

public record TranscriptionNotification(
    string FileName,
    string Status,
    DateTime ProcessedAt,
    string? TranscriptionData);
