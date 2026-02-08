using System;
using System.Collections.Generic;
using System.Text;

namespace NotificationService.Models
{
    public record MessageHandlresConfig(Dictionary<string, Type> messageHandlers);
}
