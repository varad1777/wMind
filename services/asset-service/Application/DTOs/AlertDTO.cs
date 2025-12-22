using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class AnalyzeAssetAlertsRequest
    {
        public Guid AssetId { get; set; }
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
    }

    public class RecommendedAction
    {
        public bool success { get; set; }
        public string asset { get; set; }
       public string rca { get; set; }
    }
    public class AlertDto
    {
        public Guid AlertId { get; set; }
        public string AssetName { get; set; }
        public string SignalName { get; set; }
        public DateTime AlertStartUtc { get; set; }
        public DateTime? AlertEndUtc { get; set; }
        public double MinThreshold { get; set; }
        public double MaxThreshold { get; set; }
        public double? MinObservedValue { get; set; }
        public double? MaxObservedValue { get; set; }
        public bool IsActive { get; set; }
        public bool IsAnalyzed { get; set; }
    }


}
