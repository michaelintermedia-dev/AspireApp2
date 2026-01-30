using AudioService.Models;
using Confluent.Kafka;
using static Confluent.Kafka.ConfigPropertyNames;

namespace AudioService.Services;

public class AudioProcessingService(ILogger<AudioProcessingService> logger, IConsumer<string, string> kafkaConsumer, IServiceProvider serviceProvider,
        IConfiguration configuratio) : BackgroundService
{
    private int _processCount = 0;
    private int _errorCount = 0;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const string topic = "audio.analyze.requested";

        try
        {
            kafkaConsumer.Subscribe(topic);
            logger.LogInformation("Subscribed to Kafka topic: {topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = kafkaConsumer.Consume(stoppingToken);

                    if (consumeResult.IsPartitionEOF)
                    {
                        logger.LogDebug("Reached end of partition: {partition} at offset {offset}",
                            consumeResult.Partition, consumeResult.Offset);
                        continue;
                    }
                    logger.LogDebug("Message received from topic {topic} partition {partition} at offset {offset}: {value}",
                        consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Value);

                    var message = System.Text.Json.JsonSerializer.Deserialize<MessagePayload>(consumeResult.Message.Value, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    using (var scope = serviceProvider.CreateScope())
                    {
                        var taskProcessor = scope.ServiceProvider.GetRequiredService<ITaskProcessor>();
                        await taskProcessor.ProcessAsync(stoppingToken, message.FilePath);
                    }

                    _processCount++;
                    logger.LogDebug("Processing cycle #{processNumber} completed successfully", _processCount);
                }
                catch (ConsumeException ex)
                {
                    _errorCount++;
                    logger.LogError(ex, "Kafka consume error (Error #{errorCount}): {error}", _errorCount, ex.Error.Reason);
                }
                catch (Exception ex)
                {
                    _errorCount++;
                    logger.LogError(ex, "Error processing message (Error #{errorCount})", _errorCount);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Audio Processing Service cancellation requested");
        }
        finally
        {
            kafkaConsumer.Close();
        }
    }
}


/*
using AudioService.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AudioService.Services;

public class AudioProcessingService : BackgroundService
{
    private readonly ILogger<AudioProcessingService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private int _processCount = 0;
    private int _errorCount = 0;

    public AudioProcessingService(
        ILogger<AudioProcessingService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("========== Audio Processing Service Starting ==========");
        _logger.LogInformation("Process started at {timestamp:O}", DateTime.UtcNow);

        var kafkaBootstrapServers = _configuration.GetValue<string>("Kafka:BootstrapServers", "localhost:9092");
        var kafkaTopic = _configuration.GetValue<string>("Kafka:Topic", "audio.analyze.requested");
        var kafkaGroupId = _configuration.GetValue<string>("Kafka:GroupId", "audio-processing-group");

        _logger.LogInformation("Kafka configured - Bootstrap Servers: {servers}, Topic: {topic}, GroupId: {groupId}",
            kafkaBootstrapServers, kafkaTopic, kafkaGroupId);

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBootstrapServers,
            GroupId = kafkaGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            StatisticsIntervalMs = 5000
        };

        using (var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
            {
                _logger.LogError("Kafka consumer error: {error}", e.Reason);
            })
            .SetStatisticsHandler((_, json) =>
            {
                //_logger.LogDebug("Kafka consumer statistics: {stats}", json);
            })
            .Build())
        {
            consumer.Subscribe(kafkaTopic);
            _logger.LogInformation("Subscribed to Kafka topic: {topic}", kafkaTopic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(stoppingToken);

                        if (consumeResult.IsPartitionEOF)
                        {
                            _logger.LogDebug("Reached end of partition: {partition} at offset {offset}",
                                consumeResult.Partition, consumeResult.Offset);
                            continue;
                        }
                        _logger.LogDebug("Message received from topic {topic} partition {partition} at offset {offset}: {value}",
                            consumeResult.Topic, consumeResult.Partition, consumeResult.Offset, consumeResult.Message.Value);

                        var message = System.Text.Json.JsonSerializer.Deserialize<MessagePayload>(consumeResult.Message.Value, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var taskProcessor = scope.ServiceProvider.GetRequiredService<ITaskProcessor>();
                            await taskProcessor.ProcessAsync(stoppingToken, message.FilePath);
                        }

                        _processCount++;
                        _logger.LogDebug("Processing cycle #{processNumber} completed successfully", _processCount);
                    }
                    catch (ConsumeException ex)
                    {
                        _errorCount++;
                        _logger.LogError(ex, "Kafka consume error (Error #{errorCount}): {error}", _errorCount, ex.Error.Reason);
                    }
                    catch (Exception ex)
                    {
                        _errorCount++;
                        _logger.LogError(ex, "Error processing message (Error #{errorCount})", _errorCount);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Audio Processing Service cancellation requested");
            }
            finally
            {
                consumer.Close();
            }
        }

        _logger.LogInformation("========== Audio Processing Service Stopped ==========");
        _logger.LogInformation("Total processes: {processCount}, Total errors: {errorCount}", _processCount, _errorCount);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Audio Processing Service graceful shutdown initiated");
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Audio Processing Service graceful shutdown completed");
    }
}
*/