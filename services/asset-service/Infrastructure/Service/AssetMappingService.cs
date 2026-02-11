using Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var requestedSignalIds = dto.Registers.Select(r => r.SignalTypeId).Distinct().ToList();
            var requestedRegisterAddresses = dto.Registers.Select(r => r.RegisterAddress).ToList();
            var requestedRegisterIds = dto.Registers.Select(r => r.registerId).ToList();

            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var assetSignals = await _db.AssetConfigurations
                    .Where(ac => ac.AssetId == dto.AssetId)
                    .Select(ac => ac.SignalType)
                    .ToListAsync();

                if (!assetSignals.Any())
                    throw new InvalidOperationException("No signals found for this asset.");

                var assetSignalIds = assetSignals.Select(s => s.SignalTypeID).ToHashSet();
                var invalidSignals = requestedSignalIds.Where(id => !assetSignalIds.Contains(id)).ToList();
                if (invalidSignals.Any())
                    throw new InvalidOperationException($"Requested signal(s) not found on asset: {string.Join(", ", invalidSignals)}");

                var existingMappings = await _db.MappingTable
                    .Where(m => m.AssetId == dto.AssetId && requestedSignalIds.Contains(m.SignalTypeId))
                    .ToListAsync();

                if (existingMappings.Any())
                {
                    var existingNames = existingMappings.Select(m => m.SignalName ?? m.SignalTypeId.ToString()).Distinct();
                    throw new InvalidOperationException($"Asset already has mapping(s) for signal(s): {string.Join(", ", existingNames)}");
                }

                var registerConflicts = await _db.MappingTable
                    .Where(m => m.DeviceId == dto.DeviceId && m.DevicePortId == dto.DevicePortId && requestedRegisterAddresses.Contains(m.RegisterAdress))
                    .ToListAsync();

                if (registerConflicts.Any())
                {
                    var usedAddresses = registerConflicts.Select(m => m.RegisterAdress.ToString()).Distinct();
                    throw new InvalidOperationException($"Register(s) already in use on this device port: {string.Join(", ", usedAddresses)}");
                }

                var mappings = new List<AssetSignalDeviceMapping>();
                var signals = new List<Signal>();

                foreach (var reg in dto.Registers)
                {
                    var signalType = assetSignals.First(s => s.SignalTypeID == reg.SignalTypeId);

                    var mapping = new AssetSignalDeviceMapping
                    {
                        AssetId = dto.AssetId,
                        SignalTypeId = signalType.SignalTypeID,
                        DeviceId = dto.DeviceId,
                        DevicePortId = dto.DevicePortId,
                        RegisterAdress = reg.RegisterAddress,
                        registerId = reg.registerId,
                        SignalName = signalType.SignalName,
                        SignalUnit = signalType.SignalUnit,
                        CreatedAt = DateTime.UtcNow
                    };
                    mappings.Add(mapping);

            var signal = new Signal
            {
                SignalId = Guid.NewGuid(),
                SignalKey = $"{dto.AssetId}.{dto.DeviceId}.{signalType.SignalName}",
                AssetId = dto.AssetId,
                DeviceId = dto.DeviceId,
                SignalTypeId = signalType.SignalTypeID, 
                SignalName = signalType.SignalName,
                Unit = signalType.SignalUnit,
                CreatedAt = DateTime.UtcNow
            };


                    signals.Add(signal);
                }

                _db.MappingTable.AddRange(mappings);
                _db.Signals.AddRange(signals);
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
            await using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                var assetExists = await _db.Assets.AnyAsync(a => a.AssetId == assetId);
                if (!assetExists)
                    throw new Exception("Asset not found.");

                var mappings = await _db.MappingTable
                                        .Where(m => m.AssetId == assetId)
                                        .ToListAsync();

                if (!mappings.Any())
                    throw new Exception("No device mapped to this asset.");

                var deviceIds = mappings.Select(m => m.DeviceId).Distinct().ToList();

                var signalsToDelete = await _db.Signals
                    .Where(s => s.AssetId == assetId && deviceIds.Contains(s.DeviceId))
                    .ToListAsync();

                if (signalsToDelete.Any())
                {
                    _db.Signals.RemoveRange(signalsToDelete);
                }

                _db.MappingTable.RemoveRange(mappings);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
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

        public async Task<List<Signal>> GetSignalsByAsset(Guid assetId)
        {
            try
            {
                var signals = await _db.Signals
                    .Where(s => s.AssetId == assetId)
                    .ToListAsync();

                return signals;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while fetching signals for asset", ex);
            }
        }
    }
}