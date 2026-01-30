using AudioService.Services;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Kafka Producer
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = context.Configuration.GetValue<string>("Kafka:BootstrapServers", "localhost:9092")
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        // Register HTTP Client for TaskProcessor
        services.AddHttpClient<ITaskProcessor, TaskProcessor>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });

        services.AddHostedService<AudioProcessingService>();
    });

var host = builder.Build();

// Log application startup
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("===== AudioService Starting =====");
logger.LogInformation("Environment: {environment}", host.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
logger.LogInformation("Timestamp: {timestamp:O}", DateTime.UtcNow);

await host.RunAsync();

logger.LogInformation("===== AudioService Stopped =====");
