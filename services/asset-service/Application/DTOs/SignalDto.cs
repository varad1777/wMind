
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class SignalDto
{
    public Guid SignalId { get; set; }
    public string SignalKey { get; set; }
    public Guid AssetId { get; set; }
    public Guid DeviceId { get; set; }
    public string SignalName { get; set; }
    public string Unit { get; set; }
    public Guid SignalTypeId { get; set; }
}
}