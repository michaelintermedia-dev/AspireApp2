using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI
{
    public static class Endpoints
    {
        public static void MapEndpoints(this WebApplication app)
        {
            app.MapGet("/hello", () => "Hello, World!");

            app.MapPost("/UploadAudio", async (HttpRequest request, [FromServices] IUploadService uploadService) =>
            {
                if (!request.HasFormContentType)
                {
                    return Results.BadRequest(new { message = "Expected multipart/form-data request" });
                }

                var form = await request.ReadFormAsync();
                if (form?.Files == null || form.Files.Count == 0)
                {
                    return Results.BadRequest(new { message = "No file provided" });
                }

                var file = form.Files[0];
                var (success, message, recordingId) = await uploadService.UploadAudioAsync(file);

                if (!success)
                {
                    return Results.BadRequest(new { message });
                }

                return Results.Ok(new { message, recordingId });
            });

            app.MapGet("/GetRecordings", async (IDbService dbService) =>
            {
                try
                {
                    var recordings = await dbService.GetAllRecordingsAsync();
                    return Results.Ok(recordings);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = ex.Message });
                }
            });

            app.MapGet("/DownloadAudio/{filename}", (string filename) =>
            {
                try
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                    var filePath = Path.Combine(uploadsFolder, filename);

                    // Prevent directory traversal attacks
                    var fullPath = Path.GetFullPath(filePath);
                    var fullUploadsPath = Path.GetFullPath(uploadsFolder);

                    if (!fullPath.StartsWith(fullUploadsPath))
                    {
                        return Results.BadRequest(new { message = "Invalid file path" });
                    }

                    if (!File.Exists(filePath))
                    {
                        return Results.NotFound(new { message = "File not found" });
                    }

                    return Results.File(filePath, "application/octet-stream", filename);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { message = $"Download failed: {ex.Message}" });
                }
            });

            app.MapPost("/devices/register", async (RegisterDeviceRequest request, [FromServices] IDeviceService deviceService) =>
            {
                await deviceService.RegisterDeviceAsync(request.token, request.platform);
                return Results.Ok(new { success = true });
            });
        }
    }

    record RegisterDeviceRequest(string token, string platform);
}




/*
 
 
 // Topic: user.registered
{
  "userId": "user123",
  "deviceTokens": ["token1", "token2"],
  "platform": "android",
  "registeredAt": "2024-01-06T10:00:00Z"
}

// Topic: audio.analyze.completed
{
  "userId": "user123",
  "audioId": "audio456",
  "analysisResult": "recognized: 'hello'",
  "deviceTokens": ["token1", "token2"],
  "completedAt": "2024-01-06T10:05:00Z"
}
 
 // Topic: user.deregistered
{
  "userId": "user123",
  "deviceTokens": ["token1", "token2"],
  "deregisteredAt": "2024-01-06T10:00:00Z"
}
 
 */