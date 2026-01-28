using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class Device
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Token { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public DateTime RegisteredAt { get; set; }

    public DateTime LastUsedAt { get; set; }

    public bool IsActive { get; set; }
}
