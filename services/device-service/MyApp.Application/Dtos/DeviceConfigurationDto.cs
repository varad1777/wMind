using System.ComponentModel.DataAnnotations;
using MyApp.Domain.Entities;

namespace MyApp.Application.Dtos
{
    public class DeviceConfigurationDto
    {
        [Required(ErrorMessage = "Configuration name is required.")]
        [StringLength(100, MinimumLength = 1)]
        public string Name { get; set; } = null!;

        // 🔹 NEW
        [Required]
        public DeviceProtocol Protocol { get; set; }

        // 🔹 OPC UA only
        public string? ConnectionString { get; set; }

        public OpcUaConnectionMode? ConnectionMode { get; set; }

        // 🔹 Modbus → required
        // 🔹 OPC UA → required ONLY when Polling
        [Range(100, 300000)]
        public int? PollIntervalMs { get; set; }

        // 🔹 MODBUS only
        public string? IpAddress { get; set; }

        public int? Port { get; set; }

        public int? SlaveId { get; set; }

        public string? Endian { get; set; }
    }

    


}
