using Domain.Entities;
using MappingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.DBs
{
    public class DBContext : DbContext
    {
        // ✅ Use generic DbContextOptions<DBContext>
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


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Asset self relationship
            modelBuilder.Entity<Asset>()
                .HasMany(a => a.Childrens)
                .WithOne()
                .HasForeignKey(a => a.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Asset>()
                .HasIndex(a => a.Name)
                .IsUnique();

            // AssetConfiguration
            modelBuilder.Entity<AssetConfiguration>()
                .HasKey(ac => ac.AssetConfigId); // primary key

            modelBuilder.Entity<AssetConfiguration>()
                .HasOne(a => a.Asset)
                .WithMany(a => a.AssetConfigurations)
                .HasForeignKey(a => a.AssetId);

            modelBuilder.Entity<AssetConfiguration>()
                .HasOne(ac => ac.SignalType)
                .WithMany(st => st.AssetConfigurations) // navigation property in SignalType
                .HasForeignKey(ac => ac.SignaTypeID);

            modelBuilder.Entity<AssetConfiguration>()
                .HasIndex(ac => new { ac.AssetId, ac.SignaTypeID })
                .IsUnique();

            // Mapping table indexes
            modelBuilder.Entity<AssetSignalDeviceMapping>(b =>
            {
                b.HasKey(m => m.MappingId);
                // index for fast lookup by device + port (deviceSlaveId)
                b.HasIndex(m => new { m.DeviceId, m.DevicePortId }).HasDatabaseName("IX_Mapping_Device_Port");
                // index for asset/signals if you query by them
                b.HasIndex(m => new { m.AssetId, m.SignalTypeId }).HasDatabaseName("IX_Mapping_Asset_Signal");
            });

            // SignalData configuration (time-series aggregated table)
            modelBuilder.Entity<SignalData>(b =>
            {
                b.HasKey(sd => sd.SignalDataId);

                // Unique business key for a bucket so upserts are deterministic:
                // (AssetId, SignalTypeId, DeviceId, DevicePortId, BucketStartUtc)
                b.HasIndex(e => new { e.AssetId, e.SignalTypeId, e.DeviceId, e.DevicePortId, e.BucketStartUtc })
                    .IsUnique()
                    .HasDatabaseName("UX_SignalData_BucketKey");

                // Additional supporting indexes for query patterns
                b.HasIndex(e => new { e.AssetId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_Asset_Bucket");
                b.HasIndex(e => new { e.SignalTypeId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_SignalType_Bucket");
                b.HasIndex(e => new { e.DeviceId, e.DevicePortId, e.BucketStartUtc }).HasDatabaseName("IX_SignalData_Device_Bucket");

                // Optional: define AvgValue as computed column in the database via migration if you want.
                // Example (SQL Server): ALTER TABLE ... ADD AvgValue AS (CASE WHEN Count>0 THEN Sum/Count ELSE NULL END) PERSISTED
                // EF Core migration must be edited to include the appropriate SQL for computed column.
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

                b.Property(a => a.AssetName).HasMaxLength(200).IsRequired();
                b.Property(a => a.SignalName).HasMaxLength(200).IsRequired();

                b.HasIndex(a => new { a.AssetId, a.IsAnalyzed });
                b.HasIndex(a => a.MappingId);
            });


            modelBuilder.Entity<AlertAnalysis>(b =>
            {
                b.HasKey(a => a.AlertAnalysisId);

                b.Property(a => a.RecommendedActions).IsRequired();

            });


        }
    }
}
