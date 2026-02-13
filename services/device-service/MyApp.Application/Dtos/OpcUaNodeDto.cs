// Application/Dtos/OpcUaNodeDto.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.Dtos
{
    public class OpcUaNodeDto
    {
        public Guid? OpcUaNodeId { get; set; }

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

    public class AddOpcUaNodesDto
    {
        public List<OpcUaNodeDto> Nodes { get; set; } = new();
    }
}