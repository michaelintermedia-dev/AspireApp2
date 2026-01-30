using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis")
    .WithRedisInsight();

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

var postgres = builder.AddPostgres("postgres")
.WithDataVolume()
.WithPgAdmin()
.WithHostPort(5432)
.AddDatabase("recordings");

builder.AddProject<Projects.WebAPI>("webapi")
.WithReference(redis)
.WithReference(kafka)
.WithReference(postgres)
.WithExternalHttpEndpoints()
.WaitFor(kafka);

builder.AddProject<Projects.WorkerService>("workerservice")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.AddProject<Projects.AudioService>("audioService")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();

