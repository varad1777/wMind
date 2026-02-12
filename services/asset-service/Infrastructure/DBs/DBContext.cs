using Domain.Entities;
using MappingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.DBs
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<SignalTypes> SignalTypes { get; set; } = null!;
        public DbSet<AssetConfiguration> AssetConfigurations { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationRecipient> NotificationRecipients { get; set; } = null!;
        public DbSet<AssetSignalDeviceMapping> MappingTable { get; set; } = null!;
        public DbSet<SignalData> SignalData { get; set; } = null!;
        public DbSet<ReportRequest> ReportRequests { get; set; }
        public DbSet<Alert> Alerts { get; set; } = null!;
        public DbSet<AlertAnalysis> AlertAnalyses { get; set; }
        public DbSet<Signal> Signals { get; set; } = null!;




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Asset>()
                .HasMany(a => a.Childrens)
                .WithOne()
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.Name)
                .IsUnique();

            modelBuilder.Entity<AssetConfiguration>()
                .HasKey(ac => ac.AssetConfigId);

            modelBuilder.Entity<AssetConfiguration>()
                .HasOne(a => a.Asset)
                .WithMany(a => a.AssetConfigurations)
                .HasForeignKey(a => a.AssetId);

            modelBuilder.Entity<AssetConfiguration>()
                .HasOne(ac => ac.SignalType)
                .WithMany(st => st.AssetConfigurations) 
                .HasForeignKey(ac => ac.SignaTypeID);

            modelBuilder.Entity<AssetConfiguration>()
                .HasIndex(ac => new { ac.AssetId, ac.SignaTypeID })
                .IsUnique();

            modelBuilder.Entity<AssetSignalDeviceMapping>(b =>
            {
                b.HasKey(m => m.MappingId);
                b.HasIndex(m => new { m.DeviceId, m.DevicePortId }).HasDatabaseName("IX_Mapping_Device_Port");
                b.HasIndex(m => new { m.AssetId, m.SignalTypeId }).HasDatabaseName("IX_Mapping_Asset_Signal");
            });

            modelBuilder.Entity<SignalData>(b =>
            {
                b.HasKey(sd => sd.SignalDataId);

                b.HasIndex(e => new { e.AssetId, e.SignalTypeId, e.DeviceId, e.DevicePortId, e.BucketStartUtc })
                    .IsUnique()
                    .HasDatabaseName("UX_SignalData_BucketKey");

                b.HasIndex(e => new { e.AssetId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_Asset_Bucket");
                b.HasIndex(e => new { e.SignalTypeId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_SignalType_Bucket");
                b.HasIndex(e => new { e.DeviceId, e.DevicePortId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_Device_Bucket");
 });


            modelBuilder.Entity<Notification>(b =>
            {
                b.HasKey(n => n.Id);
                b.Property(n => n.Title).HasMaxLength(250).IsRequired();
                b.Property(n => n.Text).IsRequired();
                b.Property(n => n.CreatedAt).IsRequired();
                b.Property(n => n.ExpiresAt).IsRequired();
                b.HasMany(n => n.Recipients)
                 .WithOne(r => r.Notification)
                 .HasForeignKey(r => r.NotificationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<NotificationRecipient>(b =>
            {
                b.HasKey(r => r.Id);
                b.Property(r => r.UserId).HasMaxLength(200).IsRequired();
                b.Property(r => r.CreatedAt).IsRequired();
                b.HasIndex(r => new { r.UserId, r.CreatedAt });
            });

            modelBuilder.Entity<Alert>(b =>
{
    b.HasKey(a => a.AlertId);

    b.Property(a => a.AssetName)
        .HasMaxLength(200)
        .IsRequired();

    b.Property(a => a.SignalName)
        .HasMaxLength(200)
        .IsRequired();

    // ✅ For analyzed filtering
    b.HasIndex(a => new { a.AssetId, a.IsAnalyzed });

    // ✅ LEGACY support
    b.HasIndex(a => new { a.MappingId, a.IsActive });

    // ✅ NEW (VERY IMPORTANT)
    b.HasIndex(a => new { a.SignalId, a.IsActive })
        .HasDatabaseName("IX_Alert_Signal_Active");

    // Optional but recommended
    b.HasIndex(a => a.AlertStartUtc);
});



            modelBuilder.Entity<AlertAnalysis>(b =>
            {
                b.HasKey(a => a.AlertAnalysisId);

                b.Property(a => a.RecommendedActions).IsRequired();

            });

            modelBuilder.Entity<Signal>(b =>
{
    b.HasKey(s => s.SignalId); // Primary key

    b.Property(s => s.SignalKey)
        .HasMaxLength(500)
        .IsRequired();

    b.Property(s => s.SignalName)
        .HasMaxLength(200)
        .IsRequired();

    b.Property(s => s.Unit)
        .HasMaxLength(50);

    b.Property(s => s.CreatedAt)
        .IsRequired();

    // Optional: unique index on Asset + Device + SignalKey
    b.HasIndex(s => new { s.AssetId, s.DeviceId, s.SignalKey })
     .IsUnique()
     .HasDatabaseName("UX_Signal_AssetDeviceKey");
});

            modelBuilder.Entity<Signal>(b =>
{
    b.HasKey(s => s.SignalId); 

    b.Property(s => s.SignalKey)
        .HasMaxLength(500)
        .IsRequired();

    b.Property(s => s.SignalName)
        .HasMaxLength(200)
        .IsRequired();

    b.Property(s => s.Unit)
        .HasMaxLength(50);

    b.Property(s => s.CreatedAt)
        .IsRequired();

    b.HasIndex(s => new { s.AssetId, s.DeviceId, s.SignalKey })
        .IsUnique()
        .HasDatabaseName("UX_Signal_AssetDeviceKey");

    // ✅ ADD THIS RELATIONSHIP
    b.HasOne(s => s.SignalType)
        .WithMany()
        .HasForeignKey(s => s.SignalTypeId)
        .OnDelete(DeleteBehavior.Restrict);
});



        }
    }
}