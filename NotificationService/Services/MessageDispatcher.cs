using Microsoft.Extensions.DependencyInjection;
using NotificationService.Models;
using NotificationService.Services.MessageHandlers;
using System.Reflection;
using System.Text.Json;

namespace NotificationService.Services
{
    public interface IMessageDispatcher
    {
        Task DispatchAsync(string topic, string message, CancellationToken cancellationToken);
    }

    public class MessageDispatcher : IMessageDispatcher
    {
        private readonly ILogger<MessageDispatcher> _logger;
        private readonly IServiceProvider _serviceProvider;

        public MessageDispatcher(ILogger<MessageDispatcher> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync(string topic, string message, CancellationToken cancellationToken)
        {
            try
            {
                var instance = _serviceProvider.GetRequiredKeyedService<IMessageHandler>(topic);

                if (instance == null)
                {
                    _logger.LogWarning("No handler found for topic: {topic}", topic);
                    return;
                }

                await instance.HandleMessageAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching message from topic: {topic}", topic);
                //throw;
            }
        }
    }
}
