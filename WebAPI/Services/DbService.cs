using Microsoft.EntityFrameworkCore;
using WebAPI.Models.DbData;

namespace WebAPI.Services
{
    public interface IDbService
    {
        Task<List<Recording>> GetRecordingsByUserAsync(int userId);
        Task<Recording> AddRecordingAsync(int userId, string name);
        Task SaveTranscriptionAsync(string fileName, string status, DateTime processedAt, string? transcriptionData);
    }

    public class DbService : IDbService
    {
        private readonly Recordings2Context _context;
        private readonly ILogger<DbService> _logger;

        public DbService(Recordings2Context context, ILogger<DbService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Recording>> GetRecordingsByUserAsync(int userId)
        {
            try
            {
                var recordings = await _context.Recordings
                    .Where(r => r.UserId == userId)
                    .OrderByDescending(r => r.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();

                return recordings;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch recordings: {ex.Message}", ex);
            }
        }

        public async Task<Recording> AddRecordingAsync(int userId, string name)
        {
            try
            {
                var recording = new Recording
                {
                    UserId = userId,
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Recordings.Add(recording);
                await _context.SaveChangesAsync();

                return recording;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add recording: {ex.Message}", ex);
            }
        }

        public async Task SaveTranscriptionAsync(string fileName, string status, DateTime processedAt, string? transcriptionData)
        {
            try
            {
                var recording = await _context.Recordings
                    .FirstOrDefaultAsync(r => r.Name == fileName);

                if (recording == null)
                {
                    _logger.LogWarning("Recording not found for file: {FileName}. Skipping transcription save.", fileName);
                    return;
                }

                var transcription = new Transcription
                {
                    RecordingId = recording.Id,
                    Filename = fileName,
                    ProcessedAt = processedAt,
                    TranscriptionData = transcriptionData
                };

                _context.Transcriptions.Add(transcription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Transcription saved for file: {FileName}", fileName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save transcription for {fileName}: {ex.Message}", ex);
            }
        }
    }
}