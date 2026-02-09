using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models.DbData;

public partial class Recordings2Context : DbContext
{
    public Recordings2Context()
    {
    }

    public Recordings2Context(DbContextOptions<Recordings2Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Recording> Recordings { get; set; }

    public virtual DbSet<Transcription> Transcriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserDevice> UserDevices { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum("transcription_status", new[] { "pending", "processing", "completed", "failed" });

        modelBuilder.Entity<Recording>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recordings_pkey");

            entity.ToTable("recordings");

            entity.HasIndex(e => e.UserId, "idx_recordings_user_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Recordings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("recordings_user_id_fkey");
        });

        modelBuilder.Entity<Transcription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transcriptions_pkey");

            entity.ToTable("transcriptions");

            entity.HasIndex(e => e.RecordingId, "idx_transcriptions_recording_id");

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Filename)
                .HasMaxLength(250)
                .HasColumnName("filename");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.RecordingId).HasColumnName("recording_id");
            entity.Property(e => e.TranscriptionData).HasColumnName("transcription_data");

            entity.HasOne(d => d.Recording).WithMany(p => p.Transcriptions)
                .HasForeignKey(d => d.RecordingId)
                .HasConstraintName("transcriptions_recording_id_fkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.EmailVerificationToken, "idx_users_email_verification_token").HasFilter("(email_verification_token IS NOT NULL)");

            entity.HasIndex(e => e.PasswordResetToken, "idx_users_password_reset_token").HasFilter("(password_reset_token IS NOT NULL)");

            entity.HasIndex(e => e.Email, "users_email_unique").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.EmailVerificationToken)
                .HasMaxLength(500)
                .HasColumnName("email_verification_token");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
            entity.Property(e => e.LastLoginAt).HasColumnName("last_login_at");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(500)
                .HasColumnName("password_hash");
            entity.Property(e => e.PasswordResetToken)
                .HasMaxLength(500)
                .HasColumnName("password_reset_token");
            entity.Property(e => e.PasswordResetTokenExpiry).HasColumnName("password_reset_token_expiry");
            entity.Property(e => e.PasswordSalt)
                .HasMaxLength(500)
                .HasColumnName("password_salt");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_devices_pkey");

            entity.ToTable("user_devices");

            entity.HasIndex(e => e.DeviceToken, "idx_user_devices_token");

            entity.HasIndex(e => e.UserId, "idx_user_devices_user_id");

            entity.HasIndex(e => new { e.UserId, e.DeviceToken }, "user_devices_user_token_unique").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceName)
                .HasMaxLength(200)
                .HasColumnName("device_name");
            entity.Property(e => e.DeviceToken)
                .HasMaxLength(500)
                .HasColumnName("device_token");
            entity.Property(e => e.LastActiveAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("last_active_at");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasColumnName("platform");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserDevices)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_devices_user_id_fkey");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_sessions_pkey");

            entity.ToTable("user_sessions");

            entity.HasIndex(e => e.RefreshToken, "idx_user_sessions_refresh_token");

            entity.HasIndex(e => e.UserId, "idx_user_sessions_user_id");

            entity.HasIndex(e => e.RefreshToken, "user_sessions_refresh_token_unique").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.DeviceInfo)
                .HasMaxLength(500)
                .HasColumnName("device_info");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RefreshToken)
                .HasMaxLength(500)
                .HasColumnName("refresh_token");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("user_sessions_user_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
