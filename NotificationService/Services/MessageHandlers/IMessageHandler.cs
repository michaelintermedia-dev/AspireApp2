using System;
using System.Collections.Generic;
using System.Text;

namespace NotificationService.Services.MessageHandlers
{
    public interface IMessageHandler
    {
        Task HandleMessageAsync(string message, CancellationToken cancellationToken);
    }
}
