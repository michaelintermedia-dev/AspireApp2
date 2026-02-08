using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using WebAPI.Models.DbData;
using WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpLogging;
using WebAPI.Endpoints.Endpoints;
using WebAPI.Endpoints.AuthEndpoints;
using WebAPI.Endpoints.CmsEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();  // Call this FIRST

// Now configure logging (this will work with Aspire)
builder.Logging.AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Information);

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.MediaTypeOptions.AddText("application/json");
});

builder.AddRedisDistributedCache("redis");
builder.AddKafkaProducer<string, string>("kafka");

// Add PostgreSQL DbContext
builder.Services.AddDbContext<RecordingsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("recordings")));

builder.Services.AddOpenApi();

builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDbService, DbService>();
builder.Services.AddScoped<IMessaging, Messaging>();
builder.Services.AddScoped<IDeviceService, DeviceService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddHttpClient("umbraco", client =>
{
    client.BaseAddress = new Uri("https+http://umbraco-cms");
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException()))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpLogging();
app.UseExceptionHandler(_ => { });
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

// Comment out HTTPS redirection for HTTP-only access from mobile app
// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();
app.MapAuthEndpoints();
app.MapCmsEndpoints();

app.MapHealthChecks("/healthz/live");

app.Run();

// Make Program accessible to tests
public partial class Program { }

