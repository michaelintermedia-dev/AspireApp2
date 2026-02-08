using Microsoft.Extensions.Logging;
using NotificationService.Models;
using System;
using System.Collections.Generic;
using System.Text;
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
                    _logger.LogWarning("Audio analysis event has no device tokens for user: {userId}", @event.UserId);
                    return;
                }

                var notificationData = new Dictionary<string, string>
                {
                    { "event_type", "audio_analysis_completed" },
                    { "audio_id", @event.AudioId },
                    { "user_id", @event.UserId }
                };

                await _fcmService.SendMulticastAsync(
                    @event.DeviceTokens,
                    "Audio Analysis Complete",
                    $"Your audio analysis is ready: {@event.AnalysisResult}",
                    notificationData,
                    cancellationToken);

                _logger.LogInformation(
                    "Audio analysis notification sent to user: {userId} on {deviceCount} devices",
                    @event.UserId,
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
