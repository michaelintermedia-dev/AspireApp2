using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class UserSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string RefreshToken { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? DeviceInfo { get; set; }

    public virtual User User { get; set; } = null!;
}
