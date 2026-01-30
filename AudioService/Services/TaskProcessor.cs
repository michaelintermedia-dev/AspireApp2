using System.Diagnostics;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace AudioService.Services;

public class TaskProcessor : ITaskProcessor
{
    private readonly ILogger<TaskProcessor> _logger;
    private readonly HttpClient _httpClient;
    private readonly IProducer<string, string> _kafkaProducer;

    public TaskProcessor(ILogger<TaskProcessor> logger, HttpClient httpClient, IProducer<string, string> kafkaProducer)
    {
        _logger = logger;
        _httpClient = httpClient;
        _kafkaProducer = kafkaProducer;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken, string fileName)
    {
        _logger.LogInformation("----> Processing audio tasks started");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Add your audio processing logic here
            var transcriptionResponse = await PerformAudioProcessing(cancellationToken, fileName);
            await PublishProcessingNotification(fileName, "success", transcriptionResponse, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("----> Processing audio tasks completed successfully in {milliseconds}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Audio processing was cancelled after {milliseconds}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during audio processing after {milliseconds}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<string> PerformAudioProcessing(CancellationToken cancellationToken, string fileName)
    {
        _logger.LogInformation("  >> Audio processing routine starting at {time:O}", DateTime.UtcNow);

        try
        {
            const string uploadsPath = @"C:\Projects\test\WebApplication1\WebApplication1\Uploads";
            const string transcribeUrl = "http://localhost:8000/transcribe";

            if (!Directory.Exists(uploadsPath))
            {
                _logger.LogWarning("  >> Uploads directory not found: {path}", uploadsPath);
                return string.Empty;
            }
            var filePath = Path.Combine(uploadsPath, fileName);
            var transcriptionResponse = await SendFileForTranscription(filePath, transcribeUrl, cancellationToken);

            _logger.LogInformation("  >> Audio processing routine completed at {time:O}", DateTime.UtcNow);

            return transcriptionResponse;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("  >> Audio processing routine was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  >> Exception in audio processing routine");
            throw;
        }
    }

    private async Task<string> SendFileForTranscription(string filePath, string transcribeUrl, CancellationToken cancellationToken)
    {
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            using (var content = new MultipartFormDataContent())
            {
                var fileContent = new StreamContent(fileStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/x-m4a");

                content.Add(fileContent, "file", Path.GetFileName(filePath));

                var response = await _httpClient.PostAsync(transcribeUrl, content, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                _logger.LogDebug("  >> Transcription response status: {statusCode}", response.StatusCode);
                _logger.LogDebug("  >> Transcription responseBody: {responseBody}", responseBody);

                return responseBody;
            }
        }
    }

    private async Task PublishProcessingNotification(string fileName, string status, string transcriptionData, CancellationToken cancellationToken)
    {
        try
        {
            var notificationMessage = new
            {
                fileName,
                status,
                processedAt = DateTime.UtcNow,
                transcriptionData
            };

            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(notificationMessage);

            var kafkaMessage = new Message<string, string>
            {
                Key = fileName,
                Value = jsonMessage
            };

            await _kafkaProducer.ProduceAsync("audio.analyze.completed", kafkaMessage, cancellationToken);

            _logger.LogInformation("  >> Kafka notification published for file: {fileName}", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "  >> Failed to publish Kafka notification for file: {fileName}", fileName);
            throw;
        }
    }

    private static bool IsAudioFile(string filePath)
    {
        var audioExtensions = new[] { ".m4a", ".mp3", ".wav", ".ogg", ".flac", ".aac" };
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return audioExtensions.Contains(extension);
    }
}
