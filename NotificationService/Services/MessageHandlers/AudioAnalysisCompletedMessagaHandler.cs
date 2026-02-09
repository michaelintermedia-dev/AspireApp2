using Microsoft.Extensions.Logging;
using NotificationService.Models;
using System.Text.Json;

namespace NotificationService.Services.MessageHandlers
{
    public class AudioAnalysisCompletedMessagaHandler : IMessageHandler
    {
        private readonly ILogger<AudioAnalysisCompletedMessagaHandler> _logger;
        private readonly IFcmService _fcmService;

        public AudioAnalysisCompletedMessagaHandler(ILogger<AudioAnalysisCompletedMessagaHandler> logger, IFcmService fcmService)
        {
            _logger = logger;
            _fcmService = fcmService;
        }

        public async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                var @event = JsonSerializer.Deserialize<AudioAnalysisCompletedEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (@event == null)
                {
                    _logger.LogWarning("Failed to deserialize AudioAnalysisCompletedEvent");
                    return;
                }

                if (@event.DeviceTokens.Count == 0)
                {
                    _logger.LogWarning("Audio analysis event has no device tokens for file: {FileName}", @event.FileName);
                    return;
                }

                var notificationData = new Dictionary<string, string>
                {
                    { "event_type", "audio_analysis_completed" },
                    { "file_name", @event.FileName },
                    { "status", @event.Status }
                };

                var title = "Audio Analysis Complete";
                var body = @event.Status == "success"
                    ? $"Your audio '{@event.FileName}' has been transcribed."
                    : $"Processing of '{@event.FileName}' finished with status: {@event.Status}";

                if (@event.DeviceTokens.Count == 1)
                {
                    await _fcmService.SendNotificationAsync(
                        @event.DeviceTokens[0],
                        title,
                        body,
                        notificationData,
                        cancellationToken);
                }
                else
                {
                    await _fcmService.SendMulticastAsync(
                        @event.DeviceTokens,
                        title,
                        body,
                        notificationData,
                        cancellationToken);
                }

                _logger.LogInformation(
                    "Audio analysis notification sent for file: {FileName} to {TokenCount} device(s)",
                    @event.FileName,
                    @event.DeviceTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling audio analysis completed event");
                throw;
            }
        }
    }
}
