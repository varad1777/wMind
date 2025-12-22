using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public class MappingInfo
    {
        public  Guid MappingId { get; set; }
        public Guid AssetId { get; init; }
        public Guid DeviceId { get ; set; } 
        public Guid DeviceSlaveID { get ; set; }
        public Guid SignalTypeId { get; init; }
        public string SignalName { get; init; }
        public string SignalUnit { get; init; }
       
        public int RegisterAddress { get; init; }
    }

    public interface IMappingCache
    {
        bool TryGet(Guid deviceId, Guid devicePortId,int RegisterAdress, out MappingInfo mapping);
        Task RefreshAsync(CancellationToken ct = default);
    }
}
