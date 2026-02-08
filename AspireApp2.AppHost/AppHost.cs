using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

var postgres = builder.AddPostgres("postgres")
.WithDataVolume()
.WithPgAdmin(c =>
{
    c.WithLifetime(ContainerLifetime.Persistent);
    c.WithHostPort(52653);
})
.WithHostPort(5432)
.WithLifetime(ContainerLifetime.Persistent)
.AddDatabase("recordings");

var whisperApi = builder.AddContainer("whisper-api", "whisper-api")
    .WithDockerfile("../whisper-api")  // Path to your whisper-api folder
    .WithHttpEndpoint(port: 8000, targetPort: 8000, name: "http")
    .WithEnvironment("MODEL_SIZE", "base")  // Optional: configure Whisper model size
    .WithBindMount("../whisper-api", "/app")  // For development hot reload
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExternalHttpEndpoints();


builder.AddProject<Projects.WebAPI>("webapi")
.WithReference(redis)
.WithReference(kafka)
.WithReference(postgres)
.WithExternalHttpEndpoints()
.WaitFor(kafka);

builder.AddProject<Projects.AudioService>("audioService")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WaitFor(whisperApi);

builder.Build().Run();

