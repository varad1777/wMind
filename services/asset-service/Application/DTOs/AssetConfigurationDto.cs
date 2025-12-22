using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Entities;

namespace Application.DTOs
{
    public class AssetConfigurationDto
    {
        public Guid AssetId { get; set; }

        public List<Guid> Signals { get; set; } = new List<Guid>();
    }
}
