using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;
using WebAPI.Models.DbData;

namespace WebAPI.Services
{
    public interface IDeviceService
    {
        Task RegisterDeviceAsync(int userId, string token, string platform);
    }
    public class DeviceService : IDeviceService
    {
        private readonly Recordings2Context _context;
        private readonly IMessaging _messaging;

        public DeviceService(Recordings2Context context, IMessaging messaging)
        {
            _context = context;
            _messaging = messaging;
        }
        public async Task RegisterDeviceAsync(int userId, string token, string platform)
        {
            var existingDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceToken == token);

            if (existingDevice != null)
            {
                existingDevice.LastActiveAt = DateTime.UtcNow;
            }
            else
            {
                _context.UserDevices.Add(new UserDevice
                {
                    UserId = userId,
                    DeviceToken = token,
                    Platform = platform,
                    LastActiveAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            var message = JsonSerializer.Serialize(new UserRegisteredEvent 
            {
                DeviceToken = token, 
                Platform = platform, 
                RegisteredAt = DateTime.UtcNow, 
                UserId = userId.ToString() 
            });

            await _messaging.SendMessageAsync("user.registered", message);
        }
    }
}
