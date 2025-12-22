using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ReportRequest
    {
        [Key]
        public Guid ReportId { get; set; }

        public Guid AssetId { get; set; }
        public string AssetName { get; set; }

        public string SignalIds { get; set; } // comma separated

        public string FileName { get; set; }
        public string FilePath { get; set; }

        public string Status { get; set; } // Pending / Completed / Failed

        public DateTime RequestedAt { get; set; }
    }
}
