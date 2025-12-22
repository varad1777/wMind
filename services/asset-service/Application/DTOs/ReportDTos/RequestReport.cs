using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs.ReportDTos
{
    public class RequestReport
    {
        public Guid AssetID { get; set; }
        public List<Guid> SignalIDs { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string ReportFormat { get;set; }
    }
}
