using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class InfluxTelementryDto
{
    // NEW: Primary identifier
    public Guid SignalId { get; set; }
    
    // Keep these for backwards compatibility and enrichment
    public Guid AssetId { get; set; }
    public Guid DeviceId { get; set; }
    public Guid SignalTypeId { get; set; }
    
    // Signal details
    public string SignalType { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; }
    public DateTime Timestamp { get; set; }
    
    // DEPRECATED - No longer needed with SignalId approach
    // public Guid deviceSlaveId { get; set; }
    // public Guid MappingId { get; set; }
    // public int RegisterAddress { get; set; }
}
}
