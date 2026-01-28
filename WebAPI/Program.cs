using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Confluent.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");
builder.AddKafkaProducer<string, string>("kafka");
builder.AddKafkaConsumer<string, string>("kafka", consumerBuilder =>
{
    consumerBuilder.Config.GroupId = "weather-consumer-group";
    consumerBuilder.Config.AutoOffsetReset = Confluent.Kafka.AutoOffsetReset.Earliest;
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async ([FromServices] IDistributedCache cache, [FromServices] IProducer<string, string> kafkaProducer) =>
{
    const string cacheKey = "weatherforecast";
    
    var cachedData = await cache.GetStringAsync(cacheKey);
    if (cachedData is not null)
    {
        return JsonSerializer.Deserialize<WeatherForecast[]>(cachedData);
    }

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    
    var cacheOptions = new DistributedCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    
    await cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(forecast), cacheOptions);
    
    // Publish message to Kafka
    try
    {
        var message = new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = $"New weather forecast generated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC"
        };
        
        var result = await kafkaProducer.ProduceAsync("weather-forecast-events", message);
        Console.WriteLine($"Message published to Kafka: {message.Value} (Partition: {result.Partition}, Offset: {result.Offset})");
    }
    catch (ProduceException<string, string> ex)
    {
        Console.WriteLine($"Failed to publish message to Kafka: {ex.Error.Reason}");
    }
    
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

