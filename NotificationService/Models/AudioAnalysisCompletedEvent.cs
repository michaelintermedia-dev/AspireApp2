namespace NotificationService.Models
{
    public class AudioAnalysisCompletedEvent
    {
        public string FileName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string? TranscriptionData { get; set; }
        public List<string> DeviceTokens { get; set; } = [];
    }
}
