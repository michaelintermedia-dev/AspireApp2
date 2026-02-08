using System;
using System.Collections.Generic;
using System.Text;

namespace NotificationService.Services.MessageHandlers
{
    public class TestHandler : IMessageHandler
    {
        private readonly ILogger<TestHandler> _logger;

        public TestHandler(ILogger<TestHandler> logger)
        {
            _logger = logger;
        }
        public async Task HandleMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (false)
            {

                throw new Exception();
            }
            _logger.LogInformation("TestHandler received message: {message}", message);
        }
    }
}
