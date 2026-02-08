using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebAPI.Models.DbData;

public partial class RecordingsContext : DbContext
{
    public RecordingsContext()
    {
    }

    public RecordingsContext(DbContextOptions<RecordingsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Device> Devices { get; set; }

    public virtual DbSet<Recording> Recordings { get; set; }

    public virtual DbSet<Transcription> Transcriptions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Userdevice> Userdevices { get; set; }

    public virtual DbSet<Usersession> Usersessions { get; set; }

//    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
//#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//        => optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=recordings;Username=postgres;Password=5z(yjG.A0r9DpeZTU_q3Rz");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("devices_pkey");

            entity.ToTable("devices");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.LastUsedAt).HasColumnName("last_used_at");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasColumnName("platform");
            entity.Property(e => e.RegisteredAt).HasColumnName("registered_at");
            entity.Property(e => e.Token)
                .HasMaxLength(500)
                .HasColumnName("token");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<Recording>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("recordings_pkey");

            entity.ToTable("recordings");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Date).HasColumnName("date");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Transcription>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("transcriptions_pkey");

            entity.ToTable("transcriptions");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Filename)
                .HasMaxLength(250)
                .HasColumnName("filename");
            entity.Property(e => e.Processedat).HasColumnName("processedat");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.Transcriptiondata).HasColumnName("transcriptiondata");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "idx_users_email");

            entity.HasIndex(e => e.Emailverificationtoken, "idx_users_email_verification_token");

            entity.HasIndex(e => e.Passwordresettoken, "idx_users_password_reset_token");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnName("createdat");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.Emailverificationtoken)
                .HasMaxLength(500)
                .HasColumnName("emailverificationtoken");
            entity.Property(e => e.Firstname)
                .HasMaxLength(100)
                .HasColumnName("firstname");
            entity.Property(e => e.Isemailverified)
                .HasDefaultValue(false)
                .HasColumnName("isemailverified");
            entity.Property(e => e.Lastloginat).HasColumnName("lastloginat");
            entity.Property(e => e.Lastname)
                .HasMaxLength(100)
                .HasColumnName("lastname");
            entity.Property(e => e.Passwordhash)
                .HasMaxLength(500)
                .HasColumnName("passwordhash");
            entity.Property(e => e.Passwordresettoken)
                .HasMaxLength(500)
                .HasColumnName("passwordresettoken");
            entity.Property(e => e.Passwordresettokenexpiry).HasColumnName("passwordresettokenexpiry");
            entity.Property(e => e.Passwordsalt)
                .HasMaxLength(500)
                .HasColumnName("passwordsalt");
            entity.Property(e => e.Updatedat)
                .HasDefaultValueSql("now()")
                .HasColumnName("updatedat");
        });

        modelBuilder.Entity<Userdevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("userdevices_pkey");

            entity.ToTable("userdevices");

            entity.HasIndex(e => e.Devicetoken, "idx_user_devices_token");

            entity.HasIndex(e => e.Userid, "idx_user_devices_user_id");

            entity.HasIndex(e => new { e.Userid, e.Devicetoken }, "userdevices_userid_devicetoken_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnName("createdat");
            entity.Property(e => e.Devicename)
                .HasMaxLength(200)
                .HasColumnName("devicename");
            entity.Property(e => e.Devicetoken)
                .HasMaxLength(500)
                .HasColumnName("devicetoken");
            entity.Property(e => e.Lastactiveat)
                .HasDefaultValueSql("now()")
                .HasColumnName("lastactiveat");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .HasColumnName("platform");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.Userdevices)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("userdevices_userid_fkey");
        });

        modelBuilder.Entity<Usersession>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usersessions_pkey");

            entity.ToTable("usersessions");

            entity.HasIndex(e => e.Refreshtoken, "idx_user_sessions_refresh_token");

            entity.HasIndex(e => e.Userid, "idx_user_sessions_user_id");

            entity.HasIndex(e => e.Refreshtoken, "usersessions_refreshtoken_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Createdat)
                .HasDefaultValueSql("now()")
                .HasColumnName("createdat");
            entity.Property(e => e.Deviceinfo)
                .HasMaxLength(500)
                .HasColumnName("deviceinfo");
            entity.Property(e => e.Expiresat).HasColumnName("expiresat");
            entity.Property(e => e.Refreshtoken)
                .HasMaxLength(500)
                .HasColumnName("refreshtoken");
            entity.Property(e => e.Revokedat).HasColumnName("revokedat");
            entity.Property(e => e.Userid).HasColumnName("userid");

            entity.HasOne(d => d.User).WithMany(p => p.Usersessions)
                .HasForeignKey(d => d.Userid)
                .HasConstraintName("usersessions_userid_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
