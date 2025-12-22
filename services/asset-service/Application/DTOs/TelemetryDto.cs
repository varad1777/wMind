using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Application.DTOs
{


    // --- FIXED DTO ---
    public class TelemetryDto
    {
        [JsonPropertyName("DeviceId")]
        public Guid DeviceId { get; set; }

        [JsonPropertyName("deviceSlaveId")]
        public Guid deviceSlaveId { get; set; }

        [JsonPropertyName("slaveIndex")]
        public int SlaveIndex { get; set; }

        [JsonPropertyName("RegisterAddress")]
        public int RegisterAddress { get; set; }

        [JsonPropertyName("SignalType")]
        public string SignalType { get; set; }

        [JsonPropertyName("Value")]
        public float Value { get; set; }

        [JsonPropertyName("Unit")]
        public string Unit { get; set; }

        [JsonPropertyName("Timestamp")]
        public DateTime TimestampUtc { get; set; }
    }



}
