using System.Text.Json;
using WebAPI.Models;
using WebAPI.Models.DbData;

namespace WebAPI.Services
{
    public interface IDeviceService
    {
        Task RegisterDeviceAsync(string token, string platform);
    }
    public class DeviceService : IDeviceService
    {
        private readonly IDbService _dbService;
        private readonly IMessaging _messaging;

        public DeviceService(IDbService dbService, IMessaging messaging)
        {
            _dbService = dbService;
            _messaging = messaging;
        }
        public async Task RegisterDeviceAsync(string token, string platform)
        {
            var device = new Device
            {
                Id = 0,
                Token = token,
                Platform = platform,
                RegisteredAt = DateTime.UtcNow,
                LastUsedAt = DateTime.UtcNow
            };

            await _dbService.RegisterDeviceAsync(device);

            var message = JsonSerializer.Serialize(new UserRegisteredEvent 
            {
                DeviceToken =  token, 
                Platform = platform, 
                RegisteredAt = DateTime.UtcNow, 
                UserId = "0" 
            });

            await _messaging.SendMessageAsync("user.registered", message);
        }
    }
}
