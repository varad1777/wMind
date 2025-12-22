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
        public Guid AssetId { get; set; }           // Asset ID for grouping in InfluxDB

        // Device info
        public Guid DeviceId { get; set; }          // Device ID from backend
        public Guid deviceSlaveId { get; set; }     // Device port / slave ID
        public int slaveIndex { get; set; }         // Optional if needed
        public int RegisterAddress { get; set; }    // Register address from device

        // Signal info
        public Guid SignalTypeId { get; set; }      // Mapped SignalType ID
        public string SignalType { get; set; }      // e.g., Temperature, Pressure
        public string Unit { get; set; }            // e.g., "Celsius", "Bar"

        // Measurement
        public float Value { get; set; }           // Actual telemetry value
        public DateTime Timestamp { get; set; }     // Timestamp from device / queue

        // Optional mapping ID if needed for unique identification
        public Guid MappingId { get; set; }
    }
}
