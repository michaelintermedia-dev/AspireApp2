using Confluent.Kafka;
using System.Text.Json;

namespace WebAPI.Services
{
    public interface IMessaging
    {
        Task SendMessage1Async(int audioId, string filePath, List<string> deviceTokens); 
        Task SendMessageAsync(string topic, string message);
    }

    public interface IConsumer
    {
        Task StartConsumingAsync(CancellationToken cancellationToken);
    }

    public class Messaging : IMessaging
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<Messaging> _logger;

        public Messaging(ILogger<Messaging> logger, IConfiguration configuration)
        {
            _logger = logger;

            //var config = new ProducerConfig
            //{
            //    BootstrapServers = "localhost:9092" // Update with your Kafka broker address
            //};

            var kafkaConnection = configuration.GetConnectionString("kafka") ?? "localhost:9092";

            var config = new ProducerConfig
            {
                BootstrapServers = kafkaConnection
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task SendMessage1Async(int audioId, string filePath, List<string> deviceTokens)
        {
            try
            {
                var message = new
                {
                    audioId = audioId,
                    filePath = filePath,
                    timestamp = DateTime.UtcNow,
                    deviceTokens = deviceTokens
                };

                var result = await _producer.ProduceAsync(
                    "audio.analyze.requested",
                    new Message<string, string>
                    {
                        Key = audioId.ToString(),
                        Value = JsonSerializer.Serialize(message)
                    }
                );

                _logger.LogInformation($"Message sent to Kafka topic 'audio.analyze.requested' with key {result.Key}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send Kafka message: {ex.Message}");
                throw;
            }
        }
        public async Task SendMessageAsync(string topic, string message)
        {
            try
            {


                var result = await _producer.ProduceAsync(
                    topic,
                    new Message<string, string>
                    {
                        Key = message.GetHashCode().ToString(),
                        Value = message
                    }
                );

                _logger.LogInformation($"Message sent to Kafka topic {topic} with key {result.Key}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to send Kafka message: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _producer?.Dispose();
        }
    }
}
