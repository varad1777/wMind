using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ReportDTos
{
    public class ReportQueueItem
    {
        public string AssetId { get; set; }
        public List<string> SignalIds { get; set; } 
        public List<string> MappingIds { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportFormat { get; set; } // "Excel" or "CSV"
        public int TotalRows { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
