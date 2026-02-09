using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Transcription
{
    public int Id { get; set; }

    public int RecordingId { get; set; }

    public string Filename { get; set; } = null!;

    public string? TranscriptionData { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Recording Recording { get; set; } = null!;
}
