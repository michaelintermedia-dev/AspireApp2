namespace WebAPI.Services;

public interface IUploadService
{
    Task<(bool Success, string Message, int? RecordingId)> UploadAudioAsync(int userId, IFormFile file);
}

public class UploadService : IUploadService
{
    private readonly string _uploadsFolder;
    private readonly IDbService _dbService;
    private readonly IMessaging _messaging;

    public UploadService(IDbService dbService, IMessaging messaging)
    {
        _dbService = dbService;
        _messaging = messaging;
        _uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
    }

    public async Task<(bool Success, string Message, int? RecordingId)> UploadAudioAsync(int userId, IFormFile file)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
            {
                return (false, "No file provided", null);
            }

            // Create uploads folder if it doesn't exist
            if (!Directory.Exists(_uploadsFolder))
            {
                Directory.CreateDirectory(_uploadsFolder);
            }

            // Save file to disk
            var filePath = Path.Combine(_uploadsFolder, file.FileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save metadata to database using DbService
            var recording = await _dbService.AddRecordingAsync(userId, file.FileName);
            await _messaging.SendMessage1Async(recording.Id, file.FileName);

            return (true, "Uploaded successfully!", recording.Id);
        }
        catch (Exception ex)
        {
            return (false, $"Upload failed: {ex.Message}", null);
        }
    }
}