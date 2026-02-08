using Microsoft.Extensions.Logging;
using NotificationService.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace NotificationService.Services.MessageHandlers
{
    public class UserDeregisteredMessagaHandler : IMessageHandler
    {
        private readonly ILogger<UserDeregisteredMessagaHandler> _logger;
        private readonly IFcmService _fcmService;

        public UserDeregisteredMessagaHandler(ILogger<UserDeregisteredMessagaHandler> logger, IFcmService fcmService)
        {
            _logger = logger;
            _fcmService = fcmService;
        }

        public async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (!root.TryGetProperty("userId", out var userIdElement) || userIdElement.ValueKind == JsonValueKind.Null)
                {
                    _logger.LogWarning("User deregistered event has no userId");
                    return;
                }

                var userId = userIdElement.GetString();
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User deregistered event has empty userId");
                    return;
                }

                var deviceTokens = new List<string>();
                if (root.TryGetProperty("deviceTokens", out var tokensElement) && tokensElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var token in tokensElement.EnumerateArray())
                    {
                        if (token.ValueKind == JsonValueKind.String)
                        {
                            var tokenString = token.GetString();
                            if (!string.IsNullOrEmpty(tokenString))
                            {
                                deviceTokens.Add(tokenString);
                            }
                        }
                    }
                }

                if (deviceTokens.Count > 0)
                {
                    var notificationData = new Dictionary<string, string>
                    {
                        { "event_type", "user_deregistered" },
                        { "user_id", userId }
                    };

                    await _fcmService.SendMulticastAsync(
                        deviceTokens,
                        "Account Deregistered",
                        "Your account has been deregistered",
                        notificationData,
                        cancellationToken);

                    _logger.LogInformation(
                        "User deregistration notification sent to user: {userId} on {deviceCount} devices",
                        userId,
                        deviceTokens.Count);
                }
                else
                {
                    _logger.LogInformation("User deregistered: {userId} (no active devices)", userId);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user deregistered event");
                throw;
            }
        }
    }
}
