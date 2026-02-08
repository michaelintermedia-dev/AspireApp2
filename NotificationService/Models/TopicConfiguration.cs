namespace NotificationService.Models;

public class TopicConfiguration
{
    public string[] Topics { get; }

    public TopicConfiguration(string[] topics)
    {
        Topics = topics ?? throw new ArgumentNullException(nameof(topics));
    }
}