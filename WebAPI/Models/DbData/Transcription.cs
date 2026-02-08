using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Transcription
{
    public int Id { get; set; }

    public string Filename { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? Processedat { get; set; }

    public string? Transcriptiondata { get; set; }
}
