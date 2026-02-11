using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using MappingService.Domain.Entities;

namespace Application.Interface
{
    public interface IMappingService
    {
        /// <summary>
        /// Creates mappings between an asset and a device and automatically generates corresponding Signals.
        /// </summary>
        Task<List<AssetSignalDeviceMapping>> CreateMapping(CreateMappingDto dto);

        /// <summary>
        /// Returns all asset-device mappings.
        /// </summary>
        Task<List<AssetSignalDeviceMapping>> GetMappings();

        /// <summary>
        /// Unassigns all devices from an asset and deletes the corresponding signals.
        /// </summary>
        Task UnassignDevice(Guid assetId);

        /// <summary>
        /// Gets all signals mapped to a specific asset.
        /// </summary>
        Task<List<AssetSignalDeviceMapping>> GetSignalsOnAnAsset(Guid assetId);

        /// <summary>
        /// Deletes a specific mapping by its ID and optionally removes the corresponding signal.
        /// </summary>
        Task<bool> DeleteMappingAsync(Guid mappingId);

        /// <summary>
        /// Optional: Get all signals for an asset.
        /// </summary>
        Task<List<Domain.Entities.Signal>> GetSignalsByAsset(Guid assetId);
    }
}
