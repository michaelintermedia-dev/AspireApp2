using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class UserDevice
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string DeviceToken { get; set; } = null!;

    public string Platform { get; set; } = null!;

    public string? DeviceName { get; set; }

    public DateTime LastActiveAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
