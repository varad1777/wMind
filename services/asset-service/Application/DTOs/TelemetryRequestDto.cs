using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Enums;

namespace Application.DTOs
{
    public class TelemetryRequestDto
    {
        public Guid AssetId { get; set; }
        public Guid SignalTypeId { get; set; }

        // Time range option
        public TimeRange TimeRange { get; set; } = TimeRange.LastHour;

        // Custom date range (only used when TimeRange = Custom)
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
