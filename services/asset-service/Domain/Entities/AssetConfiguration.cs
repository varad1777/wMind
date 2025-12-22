using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class AssetConfiguration
    {
        [Key]
        public Guid AssetConfigId { get; set; } = Guid.NewGuid();

        public Guid AssetId { get; set; }

        public Guid SignaTypeID { get; set; }

        public Asset Asset { get; set; }

        public SignalTypes SignalType { get; set; }
 
    }
}
