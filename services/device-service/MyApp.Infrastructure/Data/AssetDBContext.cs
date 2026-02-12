using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Entities;

namespace MyApp.Infrastructure.Data
{
    /// <summary>
    /// Read-only DbContext for Asset Database (TmindDB2Asset)
    /// Used to query Signal mappings from Device Service
    /// </summary>
    public class AssetDbContextForDevice : DbContext
    {
        public AssetDbContextForDevice(DbContextOptions<AssetDbContextForDevice> options) : base(options)
        {
        }

        public DbSet<Signal> Signals { get; set; }
        public DbSet<AssetSignalDeviceMapping> MappingTable { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Signal entity
            modelBuilder.Entity<Signal>(entity =>
            {
                entity.ToTable("Signals");
                entity.HasKey(e => e.SignalId);
                entity.Property(e => e.SignalKey).IsRequired();
                entity.Property(e => e.SignalName).IsRequired();
            });

            // Configure MappingTable entity
            modelBuilder.Entity<AssetSignalDeviceMapping>(entity =>
            {
                entity.ToTable("MappingTable");
                entity.HasKey(e => e.MappingId);
            });
        }
    }
}


