using System;
using System.Collections.Generic;

namespace WebAPI.Models.DbData;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string PasswordSalt { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public bool IsEmailVerified { get; set; }

    public string? EmailVerificationToken { get; set; }

    public string? PasswordResetToken { get; set; }

    public DateTime? PasswordResetTokenExpiry { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public virtual ICollection<Recording> Recordings { get; set; } = new List<Recording>();

    public virtual ICollection<UserDevice> UserDevices { get; set; } = new List<UserDevice>();

    public virtual ICollection<UserSession> UserSessions { get; set; } = new List<UserSession>();
}
