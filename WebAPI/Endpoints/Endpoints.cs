using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Services;

namespace WebAPI.Endpoints.Endpoints;

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

            var userIdClaim = request.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            var file = form.Files[0];
            var (success, message, recordingId) = await uploadService.UploadAudioAsync(userId, file);

            if (!success)
            {
                return Results.BadRequest(new { message });
            }

            return Results.Ok(new { message, recordingId });
        })
            .RequireAuthorization();

        app.MapGet("/GetRecordings", async (HttpContext httpContext, IDbService dbService) =>
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            try
            {
                var recordings = await dbService.GetRecordingsByUserAsync(userId);
                return Results.Ok(recordings);
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
        })
            .RequireAuthorization();

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
        })
            .RequireAuthorization();

        app.MapPost("/devices/register", async (HttpContext httpContext, RegisterDeviceRequest request, [FromServices] IDeviceService deviceService) =>
        {
            var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Results.Unauthorized();
            }

            await deviceService.RegisterDeviceAsync(userId, request.token, request.platform);
            return Results.Ok(new { success = true });
        })
            .RequireAuthorization();
    }
}

record RegisterDeviceRequest(string token, string platform);




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