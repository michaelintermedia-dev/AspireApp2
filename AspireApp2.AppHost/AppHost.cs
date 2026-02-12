
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
.AddDatabase("recordings2");

var whisperApi = builder.AddContainer("whisper-api", "whisper-api")
    .WithDockerfile("../whisper-api")  // Path to your whisper-api folder
    .WithHttpEndpoint(port: 8000, targetPort: 8000, name: "http")
    .WithEnvironment("MODEL_SIZE", "base")  // Optional: configure Whisper model size
    .WithBindMount("../whisper-api", "/app")  // For development hot reload
    .WithLifetime(ContainerLifetime.Persistent)
    .WithExternalHttpEndpoints();

// SQL Server for Umbraco CMS
var sqlServer = builder.AddSqlServer("sql")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var umbracoDb = sqlServer.AddDatabase("umbracoDbDSN");

var umbracoCms = builder.AddProject<Projects.UmbracoCms>("umbraco-cms")
    .WithReference(umbracoDb)
    .WithHttpEndpoint(port: 5190, name: "umbraco-http")
    .WithExternalHttpEndpoints()
    .WaitFor(sqlServer);

var webapi = builder.AddProject<Projects.WebAPI>("webapi")
    .WithReference(redis)
    .WithReference(kafka)
    .WithReference(postgres)
    .WithReference(umbracoCms)
    .WithHttpEndpoint(port: 5187, name: "webapi-http")
    .WithExternalHttpEndpoints()
    .WaitFor(kafka);

builder.AddProject<Projects.AudioService>("audioService")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WaitFor(whisperApi);

builder.AddProject<Projects.NotificationService>("notification-service")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();
