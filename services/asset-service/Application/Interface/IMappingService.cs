using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.DTOs;
using MappingService.Domain.Entities;
using MappingService.DTOs;

namespace Application.Interface
{
    public interface IMappingService
    {
        Task<List<AssetSignalDeviceMapping>> CreateMapping(CreateMappingDto dto);
        Task<List<AssetSignalDeviceMapping>> GetMappings();

        Task UnassignDevice(Guid AssetId);

        Task<List<AssetSignalDeviceMapping>> GetSignalsOnAnAsset(Guid assetId);
        Task<bool> DeleteMappingAsync(Guid mappingId);
    }
}
