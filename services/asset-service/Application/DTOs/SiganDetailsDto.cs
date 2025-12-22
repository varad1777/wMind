using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class SiganDetailsDto
    {
        public Guid AssetConfigID { get; set; }
        public Guid SignalTypeID { get; set; }
        
        public string SignalName { get; set; }

        public string SignalUnit { get; set; }
        public int RegsiterAdress { get; set; }

    }
}
