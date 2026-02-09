using AudioService.Services;
using Confluent.Kafka;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Override the default resilience for ALL HttpClients in this project
builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddStandardResilienceHandler(options =>
    {
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(5);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(10);
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
{
    consumerBuilder.Config.GroupId = "audio-processing-group";
    consumerBuilder.Config.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
    consumerBuilder.Config.EnableAutoCommit = true;
    consumerBuilder.Config.StatisticsIntervalMs = 5000;
});

builder.AddKafkaProducer<string, string>("kafka");

builder.Services.AddHostedService<AudioProcessingService>();
builder.Services.AddHttpClient<ITaskProcessor, TaskProcessor>()
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });

var host = builder.Build();
host.Run();

