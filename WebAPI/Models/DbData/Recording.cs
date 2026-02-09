using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Recording
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Transcription> Transcriptions { get; set; } = new List<Transcription>();

    public virtual User User { get; set; } = null!;
}
