using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Configuration
{
    public class InfluxDbOptions
    {
        public string InfluxUrl { get; set; }
        public string InfluxToken { get; set; }
        public string InfluxOrg { get; set; }
        public string InfluxBucket { get; set; }
        public int InfluxTimout { get; set; }

        public int ExcelMaxRows { get; set; }
        public int CsvMaxRows { get; set; }
    }
}
