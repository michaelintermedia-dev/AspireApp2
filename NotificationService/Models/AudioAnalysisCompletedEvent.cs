namespace NotificationService.Models
{
    public class AudioAnalysisCompletedEvent
    {
        public string UserId { get; set; } = string.Empty;
        public string AudioId { get; set; } = string.Empty;
        public string AnalysisResult { get; set; } = string.Empty;
        public string DeviceToken { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
    }
}




public class Rootobject
{
    public string fileName { get; set; }
    public string status { get; set; }
    public DateTime processedAt { get; set; }
    public string transcriptionData { get; set; }
}
