using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AlertAnalysis
    {
        public Guid AlertAnalysisId { get; set; }

       

        public Guid AssetId { get; set; }
        public string AssetName { get; set; }

        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }

        public string RecommendedActions { get; set; }

        public DateTime AnalyzedAtUtc { get; set; }
    }
}
