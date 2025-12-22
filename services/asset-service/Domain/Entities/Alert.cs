using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Alert
    {
        public Guid AlertId { get; set; }

        public Guid AssetId { get; set; }
        public string AssetName { get; set; }

        public Guid SignalTypeId { get; set; }
        public string SignalName { get; set; }

        public Guid MappingId { get; set; }

        public DateTime AlertStartUtc { get; set; }
        public DateTime? AlertEndUtc { get; set; }

        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }

        public double? MinObservedValue { get; set; }
        public double? MaxObservedValue { get; set; }

        public int ReminderTimeHours { get; set; } = 24;

        public bool IsActive { get; set; }
        public bool IsAnalyzed { get; set; }

        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
}
