// Domain/Entities/OpcUaNode.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyApp.Domain.Entities
{
    public class OpcUaNode
    {
        [Key]
        public Guid OpcUaNodeId { get; set; } = Guid.NewGuid();

        public Guid DeviceId { get; set; }
        
        [JsonIgnore]
        public Device Device { get; set; } = null!;

        [Required, StringLength(500)]
        public string NodeId { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string DataType { get; set; } = "Double";

        [StringLength(50)]
        public string? Unit { get; set; }

        public bool IsHealthy { get; set; } = true;
    }
}