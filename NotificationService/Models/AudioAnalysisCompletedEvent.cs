namespace NotificationService.Models
{
    public class AudioAnalysisCompletedEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string AudioId { get; set; } = string.Empty;
        public string AnalysisResult { get; set; } = string.Empty;
        public List<string> DeviceTokens { get; set; } = new();
        public DateTime CompletedAt { get; set; }
    }
}
