using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Domain.Entities
{
    public class DeviceConfiguration
    {
        [Key]
        public Guid ConfigurationId { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = null!;

        public int PollIntervalMs { get; set; } = 1000;

        // Protocol settings (previously stored as JSON)
        [Required]
        public string IpAddress { get; set; } = null!;

        public int Port { get; set; }

        public int SlaveId { get; set; }

        [Required]
        public string Endian { get; set; } = "Little"; // or Big

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
