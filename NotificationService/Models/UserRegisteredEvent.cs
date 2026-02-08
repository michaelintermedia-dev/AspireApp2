namespace NotificationService.Models
{
    public class UserRegisteredEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty; // "android" or "ios"
        public DateTime RegisteredAt { get; set; }
    }
}
