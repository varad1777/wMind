using Application.Interface;
using System;
using MappingService.Domain.Entities;
using MappingService.DTOs;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using Application.DTOs;
using Domain.Entities;

namespace Infrastructure.Services
{
    public class AssetMappingService : IMappingService
    {
        private readonly DBContext _db;

        public AssetMappingService(DBContext db)
        {
            _db = db;
        }



        public async Task<List<AssetSignalDeviceMapping>> CreateMapping(CreateMappingDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));
            if (dto.Registers == null || !dto.Registers.Any())
                throw new InvalidOperationException("No registers selected for mapping.");

            // normalize requested lists
            var requestedSignalIds = dto.Registers.Select(r => r.SignalTypeId).Distinct().ToList();
            var requestedRegisterAddresses = dto.Registers.Select(r => r.RegisterAddress).ToList();
            var requestedRegisterIds = dto.Registers.Select(r => r.registerId).ToList();

            // Start a transaction to ensure atomicity
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // load asset configurations (full objects so we can read SignalTypeID, SignalName, etc.)
                var assetSignals = await _db.AssetConfigurations
                            .Where(x => x.AssetId == dto.AssetId)
                            .Select(x => x.SignalType)
                            .ToListAsync();

                if (!assetSignals.Any())
                    throw new InvalidOperationException("No signals found for this asset.");

                // ensure requested signal ids belong to this asset
                var assetSignalIds = assetSignals.Select(s => s.SignalTypeID).ToHashSet();
                var invalidSignals = requestedSignalIds.Where(id => !assetSignalIds.Contains(id)).ToList();
                if (invalidSignals.Any())
                    throw new InvalidOperationException($"Requested signal(s) not found on asset: {string.Join(", ", invalidSignals)}");

                // Ensure the asset does not already have mapping(s) for any requested signal
                var existingMappingsForAssetSignals = await _db.MappingTable
                    .Where(m => m.AssetId == dto.AssetId && requestedSignalIds.Contains(m.SignalTypeId))
                    .ToListAsync();

                if (existingMappingsForAssetSignals.Any())
                {
                    var existingNames = existingMappingsForAssetSignals
                        .Select(m => m.SignalName ?? m.SignalTypeId.ToString())
                        .Distinct();

                    throw new InvalidOperationException($"Asset already has mapping(s) for signal(s): {string.Join(", ", existingNames)}");
                }

                // Ensure requested register addresses are not already used on the same device port
                var existingRegisterConflicts = await _db.MappingTable
                    .Where(m => m.DeviceId == dto.DeviceId
                             && m.DevicePortId == dto.DevicePortId
                             && requestedRegisterAddresses.Contains(m.RegisterAdress))
                    .ToListAsync();

                if (existingRegisterConflicts.Any())
                {
                    var usedAddresses = existingRegisterConflicts.Select(m => m.RegisterAdress.ToString()).Distinct();
                    throw new InvalidOperationException($"Register(s) already in use on this device port: {string.Join(", ", usedAddresses)}");
                }

                // Build mapping objects
                var mappings = new List<AssetSignalDeviceMapping>();
                foreach (var reg in dto.Registers)
                {
                    // find the asset signal object to copy metadata
                    var signal = assetSignals.First(s => s.SignalTypeID == reg.SignalTypeId);

                    mappings.Add(new AssetSignalDeviceMapping
                    {
                        AssetId = dto.AssetId,
                        SignalTypeId = signal.SignalTypeID,
                        DeviceId = dto.DeviceId,
                        DevicePortId = dto.DevicePortId,
                        RegisterAdress = reg.RegisterAddress,
                        registerId = reg.registerId,
                        SignalName = signal.SignalName,
                        SignalUnit = signal.SignalUnit,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                _db.MappingTable.AddRange(mappings);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();

                return mappings;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }



        public async Task<List<AssetSignalDeviceMapping>> GetMappings()
        {
            return await _db.MappingTable.ToListAsync();
        }

        public async Task UnassignDevice(Guid assetId)
        {
            try
            {
                var assetExists = await _db.Assets.AnyAsync(a => a.AssetId == assetId);
                if (!assetExists)
                    throw new Exception("Asset Not Found");

                var mappings = await _db.MappingTable
                                       .Where(m => m.AssetId == assetId)
                                       .ToListAsync();

                if (mappings.Count == 0)
                    throw new Exception("No device mapped to this asset");

                _db.MappingTable.RemoveRange(mappings);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error while unassigning device: {ex.Message}", ex);
            }
        }

        public async Task<List<AssetSignalDeviceMapping>> GetSignalsOnAnAsset(Guid assetId)
        {
            try
            {
                var assetExists = await _db.Assets.AnyAsync(a => a.AssetId == assetId);
                if (!assetExists)
                    return new List<AssetSignalDeviceMapping>();

                var mappings = await _db.MappingTable
                    .Where(m => m.AssetId == assetId)
                    .ToListAsync();

                if (!mappings.Any())
                    return new List<AssetSignalDeviceMapping>();  

                return mappings;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching mapped signals", ex);
            }
        }

        public async Task<bool> DeleteMappingAsync(Guid mappingId)
        {
            try
            {
                var mapping = await _db.MappingTable
                    .FirstOrDefaultAsync(m => m.MappingId == mappingId);

                if (mapping == null)
                    return false;

                _db.MappingTable.Remove(mapping);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw; 
            }
        }



    }
}
