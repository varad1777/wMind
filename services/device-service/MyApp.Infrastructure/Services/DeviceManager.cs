using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApp.Application.Dtos;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using System.Text.Json;

namespace MyApp.Infrastructure.Services
{
    public class DeviceManager : IDeviceManager
    {
        private readonly AppDbContext _db;
        private readonly ILogger<DeviceManager> _log;
        private readonly AssetDbContextForDevice _assetDb;
        private const int MaxAddresses = 200;
        public DeviceManager(AppDbContext db, ILogger<DeviceManager> log,
                             AssetDbContextForDevice assetDb)
        {
            _db = db;
            _log = log;
            _assetDb = assetDb;
        }

        public async Task<Guid> CreateDeviceAsync(
            CreateDeviceDto request,
            CancellationToken ct = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var name = request.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Device name is required.", nameof(request.Name));

            if (string.IsNullOrWhiteSpace(request.GatewayClientId))
                throw new ArgumentException("Gateway Client is required.",
                                            nameof(request.GatewayClientId));

            const int MaxDevices = 20;
            var currentCount =
                await _db.Devices.CountAsync(d => !d.IsDeleted, ct);

            if (currentCount >= MaxDevices)
                throw new InvalidOperationException(
                    $"Cannot create more than {MaxDevices} devices.");

            var exists =
                await _db.Devices.AsNoTracking()
                    .AnyAsync(d => !d.IsDeleted &&
                                   d.Name.ToLower() == name.ToLower(),
                              ct);

            if (exists)
                throw new InvalidOperationException(
                    $"Device name '{name}' already exists.");

            await using var tx =
                await _db.Database.BeginTransactionAsync(ct);

            var device =
                new Device
                {
                    DeviceId = Guid.NewGuid(),
                    Name = name,
                    Description =
                        string.IsNullOrWhiteSpace(request.Description)
                            ? null
                            : request.Description.Trim(),

                    GatewayId = request.GatewayClientId,
                    Protocol = request.Protocol
                };

            await _db.Devices.AddAsync(device, ct);

            // ---------------- Configuration ----------------
            if (request.Configuration != null)
            {
                var cfgDto = request.Configuration;

                if (string.IsNullOrWhiteSpace(cfgDto.Name) || cfgDto.Name.Length > 100)
                    throw new ArgumentException(
                        "Configuration name must be between 1 and 100 characters.",
                        nameof(cfgDto.Name));

                // -------- Protocol validation --------
                if (request.Protocol == DeviceProtocol.Modbus)
                {
                    if (string.IsNullOrWhiteSpace(cfgDto.IpAddress))
                        throw new ArgumentException("IpAddress is required for Modbus");

                    if (!cfgDto.Port.HasValue || cfgDto.Port <= 0 || cfgDto.Port > 65535)
                        throw new ArgumentOutOfRangeException(nameof(cfgDto.Port));

                    if (!cfgDto.SlaveId.HasValue || cfgDto.SlaveId < 0 || cfgDto.SlaveId > 247)
                        throw new ArgumentOutOfRangeException(nameof(cfgDto.SlaveId));
                }
                else if (request.Protocol == DeviceProtocol.OpcUa)
                {
                    if (string.IsNullOrWhiteSpace(cfgDto.ConnectionString))
                        throw new ArgumentException(
                            "ConnectionString is required for OPC UA");

                    if (!cfgDto.ConnectionMode.HasValue)
                        throw new ArgumentException(
                            "ConnectionMode is required for OPC UA");

                    if (cfgDto.ConnectionMode == OpcUaConnectionMode.Polling &&
                        !cfgDto.PollIntervalMs.HasValue)
                        throw new ArgumentException(
                            "PollIntervalMs is required for OPC UA Polling");
                }
                else
                {
                    throw new InvalidOperationException("Unsupported protocol");
                }

                var cfg =
                    new DeviceConfiguration
                    {
                        ConfigurationId = Guid.NewGuid(),
                        Name = cfgDto.Name.Trim(),
                        Protocol = request.Protocol,

                        // OPC UA
                        ConnectionString =
                            request.Protocol == DeviceProtocol.OpcUa
                                ? cfgDto.ConnectionString
                                : null,

                        ConnectionMode =
                            request.Protocol == DeviceProtocol.OpcUa
                                ? cfgDto.ConnectionMode
                                : null,

                        // Polling
                        PollIntervalMs =
                            request.Protocol == DeviceProtocol.Modbus
                                ? cfgDto.PollIntervalMs ?? 1000
                                : cfgDto.ConnectionMode == OpcUaConnectionMode.Polling
                                    ? cfgDto.PollIntervalMs
                                    : null,

                        // MODBUS
                        IpAddress =
                            request.Protocol == DeviceProtocol.Modbus
                                ? cfgDto.IpAddress
                                : null,

                        Port =
                            request.Protocol == DeviceProtocol.Modbus
                                ? cfgDto.Port
                                : null,

                        SlaveId =
                            request.Protocol == DeviceProtocol.Modbus
                                ? cfgDto.SlaveId
                                : null,

                        Endian =
                            request.Protocol == DeviceProtocol.Modbus
                                ? cfgDto.Endian
                                : null
                    };

                await _db.DeviceConfigurations.AddAsync(cfg, ct);
                device.DeviceConfigurationId = cfg.ConfigurationId;
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _log.LogInformation("Created device {DeviceId}", device.DeviceId);
            return device.DeviceId;
        }


        public async Task UpdateDeviceAsync(Guid deviceId, UpdateDeviceDto dto, DeviceConfigurationDto? configDto = null, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
                throw new KeyNotFoundException("Device not found");

            var isMapped = await _assetDb.MappingTable.AsNoTracking().AnyAsync(
                m => m.DeviceId == deviceId, ct);
            if (isMapped)
                throw new InvalidOperationException(
                    "Cannot update device because it is mapped to asset");

            // Prevent updates to soft-deleted devices
            if (device.IsDeleted)
                throw new InvalidOperationException("Cannot update a deleted device.");

            // ---------------- Device fields ----------------
            if (dto.Name != null)
            {
                var trimmed = dto.Name.Trim();
                if (trimmed.Length < 3 || trimmed.Length > 100)
                    throw new ArgumentException(
                        "Device name must be between 3 and 100 characters.",
                        nameof(dto.Name));

                var newNameNorm = trimmed.ToLowerInvariant();
                var currentNameNorm = (device.Name ?? string.Empty).ToLowerInvariant();

                if (newNameNorm != currentNameNorm)
                {
                    var exists = await _db.Devices.AsNoTracking().AnyAsync(
                        d => d.DeviceId != deviceId && !d.IsDeleted &&
                             d.Name.ToLower() == newNameNorm,
                        ct);

                    if (exists)
                        throw new ArgumentException(
                            "A device with the same name already exists.");
                }

                device.Name = trimmed;
            }

            if (dto.Description != null)
            {
                var trimmedDesc = dto.Description.Trim();
                if (trimmedDesc.Length > 255)
                    throw new ArgumentException(
                        "Description cannot exceed 255 characters.",
                        nameof(dto.Description));
                device.Description = trimmedDesc;
            }

            if (dto.Protocol.HasValue)
            {
                // Optional: prevent protocol change when configuration exists
                if (device.DeviceConfigurationId.HasValue)
                    throw new InvalidOperationException(
                        "Cannot change protocol when configuration already exists.");

                device.Protocol = dto.Protocol.Value;
            }


            // ---------------- Configuration ----------------
            if (configDto != null)
            {
                // ... existing validation code ...

                // Helper to apply protocol-aware fields
                void ApplyConfig(DeviceConfiguration cfg)
                {
                    cfg.Name = configDto.Name.Trim();
                    cfg.Protocol = configDto.Protocol;

                    // OPC UA
                    cfg.ConnectionString = configDto.Protocol == DeviceProtocol.OpcUa
                        ? configDto.ConnectionString
                        : null;

                    cfg.ConnectionMode = configDto.Protocol == DeviceProtocol.OpcUa
                        ? configDto.ConnectionMode
                        : null;

                    // Polling
                    cfg.PollIntervalMs = configDto.Protocol == DeviceProtocol.Modbus
                        ? configDto.PollIntervalMs ?? 1000
                        : configDto.ConnectionMode == OpcUaConnectionMode.Polling
                            ? configDto.PollIntervalMs
                            : null;

                    // MODBUS
                    cfg.IpAddress = configDto.Protocol == DeviceProtocol.Modbus ? configDto.IpAddress : null;
                    cfg.Port = configDto.Protocol == DeviceProtocol.Modbus ? configDto.Port : null;
                    cfg.SlaveId = configDto.Protocol == DeviceProtocol.Modbus ? configDto.SlaveId : null;
                    cfg.Endian = configDto.Protocol == DeviceProtocol.Modbus ? configDto.Endian : null;
                }

                DeviceConfiguration targetCfg;

                if (device.DeviceConfigurationId is Guid cfgId)
                {
                    var otherUses = await _db.Devices.AsNoTracking().AnyAsync(
                        d => d.DeviceId != deviceId && d.DeviceConfigurationId == cfgId, ct);

                    if (!otherUses)
                    {
                        targetCfg = await _db.DeviceConfigurations.FindAsync(cfgId);
                        if (targetCfg == null)
                        {
                            targetCfg = new DeviceConfiguration { ConfigurationId = Guid.NewGuid() };
                            await _db.DeviceConfigurations.AddAsync(targetCfg, ct);
                        }
                    }
                    else
                    {
                        targetCfg = new DeviceConfiguration { ConfigurationId = Guid.NewGuid() };
                        await _db.DeviceConfigurations.AddAsync(targetCfg, ct);
                    }
                }
                else
                {
                    targetCfg = new DeviceConfiguration { ConfigurationId = Guid.NewGuid() };
                    await _db.DeviceConfigurations.AddAsync(targetCfg, ct);
                }

                ApplyConfig(targetCfg);
                device.DeviceConfigurationId = targetCfg.ConfigurationId;

               
                _log.LogInformation(
                    "Attached/updated configuration {CfgId} and set device {DeviceId} protocol to {Protocol}",
                    targetCfg.ConfigurationId, deviceId, configDto.Protocol);
            }

        }

        public async Task<(List<Device> Devices, int TotalCount)> GetAllDevicesAsync(int pageNumber, int pageSize, string? searchTerm, CancellationToken ct = default)
        {
            // Start query
            var query = _db.Devices.Where(d => !d.IsDeleted)
                            .Include(d => d.DeviceConfiguration)
                            .AsNoTracking();

            // Apply search
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query =
                    query.Where(d => d.Name.ToLower().Contains(searchTerm) ||
                                     (d.Description != null &&
                                      d.Description.ToLower().Contains(searchTerm)));
            }

            // Get total count for pagination metadata
            var totalCount = await query.CountAsync(ct);

            // Apply pagination
            var devices = await query.Skip((pageNumber - 1) * pageSize)
                              .Take(pageSize)
                              .ToListAsync(ct);

            return (devices, totalCount);
        }

        public async Task DeleteDeviceAsync(Guid deviceId, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
                throw new KeyNotFoundException("Device not found");

            if (device.IsDeleted)
            {
                _log.LogWarning("Device {DeviceId} is already marked as deleted",
                                deviceId);
                return;
            }

            var isMapped = await _assetDb.MappingTable.AsNoTracking().AnyAsync(
                m => m.DeviceId == deviceId, ct);
            if (isMapped)
                throw new InvalidOperationException(
                    "Cannot delete device because it is mapped to asset");

            // Optional: If you want to prevent deletion if config is used elsewhere
            if (device.DeviceConfigurationId is Guid cfgId)
            {
                var otherUses = await _db.Devices.AsNoTracking().AnyAsync(
                    d => d.DeviceId != deviceId && d.DeviceConfigurationId == cfgId &&
                         !d.IsDeleted,
                    ct);

                if (otherUses)
                    throw new InvalidOperationException(
                        "DeviceConfiguration is referenced by other devices and cannot be deleted. Detach it first or remove other references.");
            }

            // Soft delete instead of physical delete
            device.IsDeleted = true;

            // Optionally update timestamp or audit fields here if needed
            // device.DeletedAt = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync(ct);

            _log.LogInformation("Soft deleted device {DeviceId}", deviceId);
        }
        public Task<Device?> GetDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
            _db.Devices.Include(d => d.DeviceConfiguration)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId && !d.IsDeleted,
                                     ct);

        public async Task<List<Device>> GetDeletedDevicesAsync(CancellationToken ct = default)
        {
            return await _db.Devices.Where(d => d.IsDeleted)
                .Include(d => d.DeviceConfiguration)
                .AsNoTracking()
                .ToListAsync(ct);
        }

        // --- Get one soft-deleted device
        public Task<Device?> GetDeletedDeviceAsync(Guid deviceId, CancellationToken ct = default) =>
            _db.Devices.Where(d => d.IsDeleted)
                .Include(d => d.DeviceConfiguration)
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId, ct);

        // --- Restore soft-deleted device
        public async Task RestoreDeviceAsync(Guid deviceId, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
                throw new KeyNotFoundException("Device not found");
            if (!device.IsDeleted)
            {
                _log.LogWarning(
                    "Attempted to restore device {DeviceId} but it is not deleted",
                    deviceId);
                return;  // or throw if you prefer
            }

            // If there's any business rule preventing restore (example: config
            // removed) handle here.
            device.IsDeleted = false;
            // Optionally update timestamps: device.UpdatedAt = DateTime.UtcNow;

            _db.Devices.Update(device);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation("Restored device {DeviceId}", deviceId);
        }

        // --- Permanently delete (hard delete) a device and related resources (if
        // desired)
        public async Task PermanentlyDeleteDeviceAsync(Guid deviceId, CancellationToken ct = default)
        {
            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
                throw new KeyNotFoundException("Device not found");

            // If you want only-allow-hard-delete-for-already-soft-deleted:
            // if (!device.IsDeleted) throw new InvalidOperationException("Device must
            // be soft-deleted first.");

            // Remove related child rows if cascade isn't configured (uncomment if
            // needed) var ports = _db.DeviceSlaves.Where(p => p.DeviceId ==
            // deviceId); _db.DeviceSlaves.RemoveRange(ports); var portSets =
            // _db.DeviceSlaveSets.Where(ps => ps.DeviceId == deviceId);
            // _db.DeviceSlaveSets.RemoveRange(portSets);

            if (device.DeviceConfigurationId is Guid cfgId)
            {
                // ensure no other non-deleted devices reference the same config
                var otherUses = await _db.Devices.AsNoTracking().AnyAsync(
                    d => d.DeviceId != deviceId && d.DeviceConfigurationId == cfgId &&
                         !d.IsDeleted,
                    ct);

                if (otherUses)
                {
                    // detach only device, keep config
                    _db.Devices.Remove(device);
                    await _db.SaveChangesAsync(ct);
                    _log.LogInformation(
                        "Hard-deleted device {DeviceId} but kept shared configuration {CfgId}",
                        deviceId, cfgId);
                    return;
                }

                // safe to delete the config too
                var cfg = await _db.DeviceConfigurations.FindAsync(
                    new object[] { cfgId }, ct);
                if (cfg != null)
                    _db.DeviceConfigurations.Remove(cfg);
            }

            _db.Devices.Remove(device);
            await _db.SaveChangesAsync(ct);
            _log.LogInformation(
                "Hard-deleted device {DeviceId} and its configuration if not shared",
                deviceId);
        }

        public async Task<Guid> AddConfigurationAsync(
            Guid deviceId,
            DeviceConfigurationDto dto,
            CancellationToken ct = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null)
                throw new KeyNotFoundException("Device not found");

            if (device.IsDeleted)
                throw new InvalidOperationException(
                    "Cannot attach configuration to a deleted device.");

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Name.Length > 100)
                throw new ArgumentException(
                    "Configuration name must be between 1 and 100 characters.",
                    nameof(dto.Name));

            // -------- Protocol validation --------
            if (dto.Protocol == DeviceProtocol.Modbus)
            {
                if (string.IsNullOrWhiteSpace(dto.IpAddress))
                    throw new ArgumentException("IpAddress is required for Modbus");

                if (!dto.Port.HasValue || dto.Port <= 0 || dto.Port > 65535)
                    throw new ArgumentOutOfRangeException(nameof(dto.Port));

                if (!dto.SlaveId.HasValue || dto.SlaveId < 0 || dto.SlaveId > 247)
                    throw new ArgumentOutOfRangeException(nameof(dto.SlaveId));
            }
            else if (dto.Protocol == DeviceProtocol.OpcUa)
            {
                if (string.IsNullOrWhiteSpace(dto.ConnectionString))
                    throw new ArgumentException("ConnectionString is required for OPC UA");

                if (!dto.ConnectionMode.HasValue)
                    throw new ArgumentException("ConnectionMode is required for OPC UA");

                if (dto.ConnectionMode == OpcUaConnectionMode.Polling &&
                    !dto.PollIntervalMs.HasValue)
                    throw new ArgumentException(
                        "PollIntervalMs is required for OPC UA Polling");
            }
            else
            {
                throw new InvalidOperationException("Unsupported protocol");
            }

            var cfg = new DeviceConfiguration
            {
                ConfigurationId = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Protocol = dto.Protocol,

                // OPC UA
                ConnectionString = dto.Protocol == DeviceProtocol.OpcUa
                    ? dto.ConnectionString
                    : null,

                ConnectionMode = dto.Protocol == DeviceProtocol.OpcUa
                    ? dto.ConnectionMode
                    : null,

                // Polling
                PollIntervalMs = dto.Protocol == DeviceProtocol.Modbus
                    ? dto.PollIntervalMs ?? 1000
                    : dto.ConnectionMode == OpcUaConnectionMode.Polling
                        ? dto.PollIntervalMs
                        : null,

                // MODBUS
                IpAddress = dto.Protocol == DeviceProtocol.Modbus ? dto.IpAddress : null,
                Port = dto.Protocol == DeviceProtocol.Modbus ? dto.Port : null,
                SlaveId = dto.Protocol == DeviceProtocol.Modbus ? dto.SlaveId : null,
                Endian = dto.Protocol == DeviceProtocol.Modbus ? dto.Endian : null
            };

            // Add configuration
            await _db.DeviceConfigurations.AddAsync(cfg, ct);

            // Update device config reference
            device.DeviceConfigurationId = cfg.ConfigurationId;

            // Update protocol and ensure EF tracks it
            device.Protocol = dto.Protocol;
            _db.Entry(device).Property(d => d.Protocol).IsModified = true;

            await _db.SaveChangesAsync(ct);

            _log.LogInformation(
                "Added configuration {CfgId} to device {DeviceId} and updated protocol to {Protocol}",
                cfg.ConfigurationId,
                deviceId,
                dto.Protocol);

            return cfg.ConfigurationId;

        }


        public async Task<Guid> AddPortAsync(Guid deviceId, AddPortDto dto, CancellationToken ct = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (dto.Registers.Count > 5)
                throw new InvalidOperationException(
                    "A slave can have a maximum of 5 registers.");

            var device = await _db.Devices.FindAsync(new object[] { deviceId }, ct);
            if (device == null || device.IsDeleted)
                throw new KeyNotFoundException("Device not found");

            var slaveCount =
                await _db.DeviceSlaves.CountAsync(s => s.DeviceId == deviceId, ct);
            if (slaveCount >= 2)
                throw new InvalidOperationException(
                    "A device can have a maximum of 2 slaves.");

            var exists = await _db.DeviceSlaves.AnyAsync(
                p => p.DeviceId == deviceId && p.slaveIndex == dto.slaveIndex, ct);
            if (exists)
                throw new InvalidOperationException(
                    $"Port with index {dto.slaveIndex} already exists");

            var port = new DeviceSlave
            {
                DeviceId = deviceId,
                slaveIndex = dto.slaveIndex,
                IsHealthy = dto.IsHealthy,
                Registers =
                  dto.Registers
                      .Select(r => new Register
                      {
                          RegisterAddress = r.RegisterAddress,
                          RegisterLength = r.RegisterLength,
                          DataType = r.DataType,
                          Scale = r.Scale,
                          Unit = r.Unit,
                          ByteOrder = r.ByteOrder,
                          WordSwap = r.WordSwap,
                          IsHealthy = r.IsHealthy
                      })
                      .ToList()
            };

            await _db.DeviceSlaves.AddAsync(port, ct);
            await _db.SaveChangesAsync(ct);
            return port.deviceSlaveId;
        }

        public async Task UpdatePortAsync(Guid deviceId, int slaveIndex,
                                          AddPortDto dto,
                                          CancellationToken ct = default)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Registers.Count > 5)
                throw new InvalidOperationException(
                    "A slave can have a maximum of 5 registers.");

            // 1) Load the port (no-tracking to check existence and get deviceSlaveId)
            var portNoTrack =
                await _db.DeviceSlaves.AsNoTracking().FirstOrDefaultAsync(
                    p => p.DeviceId == deviceId && p.slaveIndex == slaveIndex, ct);

            if (portNoTrack == null)
                throw new KeyNotFoundException(
                    $"Port {slaveIndex} not found for device {deviceId}.");

            // 2) Collect incoming registerIds (from each RegisterDto) and addresses
            // for reporting
            var incomingRegisterIds = dto.Registers.Where(r => r.registerId.HasValue)
                                          .Select(r => r.registerId!.Value)
                                          .Distinct()
                                          .ToList();

            var incomingAddresses =
                dto.Registers.Select(r => r.RegisterAddress).Distinct().ToList();

            // If there are no incoming registerIds, mappedRegisterIds will be empty
            List<Guid> mappedRegisterIds = new();
            if (incomingRegisterIds.Any())
            {
                // 3) Query mapping table in asset DB for mappings that match (DeviceId,
                // DevicePortId) and the incoming registerIds
                mappedRegisterIds =
                    await _assetDb.MappingTable.AsNoTracking()
                        .Where(m => m.DeviceId == deviceId &&
                                    m.DevicePortId == portNoTrack.deviceSlaveId &&
                                    incomingRegisterIds.Contains(m.registerId))
                        .Select(m => m.registerId)
                        .Distinct()
                        .ToListAsync(ct);
            }

            // 4) Determine which registers we are allowed to add/update (exclude
            // those whose registerId is mapped)
            //    - If a RegisterDto has no registerId (new register), allow it.
            //    - If it has registerId and that id is mapped, skip it.
            var registersToAdd =
                dto.Registers
                    .Where(r => !r.registerId.HasValue ||
                                !mappedRegisterIds.Contains(r.registerId.Value))
                    .ToList();

            // Start transaction for Device DB changes (atomic for the device DB)
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // 5) Delete existing registers for this port EXCEPT those that are
                // mapped.
                //    ASSUMPTION: Register entity has a Guid PK named "RegisterId".
                var existingRegistersToDelete = _db.Registers.Where(
                    r => r.deviceSlaveId == portNoTrack.deviceSlaveId &&
                         !(mappedRegisterIds.Any() &&
                           mappedRegisterIds.Contains(r.RegisterId)));

                _db.Registers.RemoveRange(existingRegistersToDelete);
                await _db.SaveChangesAsync(ct);  // apply deletes

                // 6) Re-fetch the port entity for EF change tracking
                var port = await _db.DeviceSlaves.FirstOrDefaultAsync(
                    p => p.DeviceId == deviceId && p.slaveIndex == slaveIndex, ct);

                if (port == null)
                {
                    await tx.RollbackAsync(ct);
                    throw new InvalidOperationException(
                        "Port disappeared during update; please retry.");
                }

                port.IsHealthy = dto.IsHealthy;

                // 7) Add permitted registers (those NOT having mapped registerId)
                var newRegisters =
                    registersToAdd
                        .Select(r => new Register
                        {
                            // set RegisterId only if your Register entity expects it;
                            // typically EF will generate PK for new rows
                            RegisterAddress = r.RegisterAddress,
                            RegisterLength = r.RegisterLength,
                            DataType = r.DataType,
                            Scale = r.Scale,
                            Unit = r.Unit,
                            ByteOrder = r.ByteOrder,
                            WordSwap = r.WordSwap,
                            IsHealthy = r.IsHealthy,
                            deviceSlaveId = port.deviceSlaveId
                        })
                        .ToList();

                if (newRegisters.Any())
                    await _db.Registers.AddRangeAsync(newRegisters, ct);

                await _db.SaveChangesAsync(ct);  // save adds and port update

                // commit transaction on device DB
                await tx.CommitAsync(ct);

                // 8) If there were mapped registerIds, notify caller which registerIds
                // were skipped.
                // if (mappedRegisterIds.Any())
                //{
                //    // Try to find their addresses from incoming DTO for better
                //    messaging (optional) var skippedAddresses = dto.Registers
                //        .Where(r => r.registerId.HasValue &&
                //        mappedRegisterIds.Contains(r.registerId.Value)) .Select(r =>
                //        r.RegisterAddress) .Distinct() .ToList();

                //    throw new InvalidOperationException(
                //        $"The following registers were not updated because they are
                //        mapped to assets. " + (skippedAddresses.Any() ? $"addresses:
                //        {string.Join(", ", skippedAddresses)}" : "")
                //    );
                //}
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _log.LogError(ex,
                              "Concurrency error updating port {DeviceId}/{slaveIndex}",
                              deviceId, slaveIndex);
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException(
                    "Concurrency error while updating port", ex);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error updating port {DeviceId}/{slaveIndex}",
                              deviceId, slaveIndex);
                try
                {
                    await tx.RollbackAsync(ct);
                }
                catch
                { /* swallow rollback failures to rethrow original */
                }
                throw;
            }
        }

        // optional getter
        public async Task<DeviceSlave?> GetPortAsync(Guid deviceId, int slaveIndex, CancellationToken ct = default)
        {
            return await _db.DeviceSlaves.Include(p => p.Registers)
                .FirstOrDefaultAsync(
                    p => p.DeviceId == deviceId && p.slaveIndex == slaveIndex, ct);
        }

        public async Task<List<MatchedDeviceDto>> GetDevicesMatchingRegisterAddressesAsync(int[] registerAddresses, CancellationToken ct)
        {
            if (registerAddresses == null || registerAddresses.Length == 0)
                throw new ArgumentException("registerAddresses required");

            // 1) Validate & dedupe
            var addresses = registerAddresses.Where(a => a >= 0 && a <= 65535)
                                .Distinct()
                                .ToArray();

            if (addresses.Length == 0)
                throw new ArgumentException("No valid register addresses provided.");

            if (addresses.Length > 1000)  // safety limit
                throw new ArgumentException("Too many addresses.");

            // 2) Query DB for devices that contain ALL the requested register
            // addresses
            // var devices = await _db.Devices
            //    .AsNoTracking()
            //    .Where(d => !d.IsDeleted &&
            //        d.DeviceSlave
            //         .SelectMany(ds => ds.Registers)
            //         .Where(r => addresses.Contains(r.RegisterAddress))
            //         .Select(r => r.RegisterAddress)
            //         .Distinct()
            //         .Count() == addresses.Length
            //    )
            //    .Include(d => d.DeviceSlave)
            //        .ThenInclude(ds => ds.Registers)
            //    .ToListAsync(ct);

            var devices =
                await _db.Devices.AsNoTracking()
                    .Where(d => !d.IsDeleted &&
                                d.DeviceSlave.SelectMany(ds => ds.Registers)
                                    .Any(r => addresses.Contains(r.RegisterAddress)))
                    .Include(d => d.DeviceSlave)
                    .ThenInclude(ds => ds.Registers)
                    .ToListAsync(ct);

            // 3) Map in-memory — keep only slaves & registers that match
            var result = new List<MatchedDeviceDto>();

            foreach (var d in devices)
            {
                var matchedSlaves = new List<MatchedSlaveDto>();

                foreach (var ds in d.DeviceSlave)
                {
                    var regs = ds.Registers ?? new List<Register>();

                    var matchedRegs =
                        regs.Where(r => addresses.Contains(r.RegisterAddress))
                            .Select(r => new MatchedRegisterDto
                            {
                                RegisterId = r.RegisterId,
                                RegisterAddress = r.RegisterAddress,
                                RegisterLength = r.RegisterLength,
                                DataType = r.DataType,
                                IsHealthy = r.IsHealthy,
                                Scale = r.Scale,
                                Unit = r.Unit
                            })
                            .ToList();

                    if (matchedRegs.Count > 0)
                    {
                        matchedSlaves.Add(new MatchedSlaveDto
                        {
                            DeviceSlaveId = ds.deviceSlaveId,
                            SlaveIndex = ds.slaveIndex,
                            IsHealthy = ds.IsHealthy,
                            MatchedRegisters = matchedRegs
                        });
                    }
                }

                if (matchedSlaves.Count > 0)
                {
                    result.Add(new MatchedDeviceDto
                    {
                        DeviceId = d.DeviceId,
                        Name = d.Name,
                        Description = d.Description,
                        Protocol = d.Protocol,
                        MatchedSlaves = matchedSlaves
                    });
                }
            }

            return result;
        }

        public async Task<List<DeviceSlave>> GetPortsByDeviceAsync(Guid deviceId, CancellationToken ct)
        {
            if (deviceId == Guid.Empty)
                throw new ArgumentException("Device ID cannot be empty.",
                                            nameof(deviceId));

            return await _db.DeviceSlaves.Include(p => p.Registers)
                .Where(p => p.DeviceId == deviceId)
                .ToListAsync(ct);
        }

        public async Task<BulkCreateDeviceResultDto> CreateDevicesBulkAsync(BulkCreateDeviceDto request, CancellationToken ct = default)
        {
            if (request == null || request.Devices.Count == 0)
                throw new ArgumentException("No devices provided.");

            var result = new BulkCreateDeviceResultDto();

            // Count existing active devices
            var existingCount = await _db.Devices.CountAsync(d => !d.IsDeleted, ct);
            if (existingCount + request.Devices.Count > 20)
            {
                result.Errors.Add(
                    $"Cannot create {request.Devices.Count} devices. Total devices after creation would be {existingCount + request.Devices.Count}, but maximum allowed is 20.");
                return result;
            }

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            foreach (var dto in request.Devices)
            {
                try
                {
                    var name = dto.Name?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                        throw new ArgumentException("Device name is required.");

                    // ONLY ADDITION: protocol validation
                    if (!Enum.IsDefined(typeof(DeviceProtocol), dto.Protocol))
                        throw new ArgumentException("Invalid device protocol.");


                    var exists = await _db.Devices.AsNoTracking().AnyAsync(
                        d => !d.IsDeleted && d.Name.ToLower() == name.ToLower(), ct);

                    if (exists)
                        throw new InvalidOperationException(
                            $"Device name '{name}' already exists.");

                    var device = new Device
                    {
                        DeviceId = Guid.NewGuid(),
                        Name = name,
                        Description = string.IsNullOrWhiteSpace(dto.Description)
                            ? null
                            : dto.Description.Trim(),

                        // ONLY ADDITION: assign protocol
                        Protocol = dto.Protocol
                    };

                    // Configuration (new explicit fields logic)
                    if (dto.Configuration != null)
                    {
                        var c = dto.Configuration;

                        if (string.IsNullOrWhiteSpace(c.Name) || c.Name.Length > 100)
                            throw new ArgumentException(
                                "Configuration name must be between 1 and 100 characters.");

                        // -------- Protocol validation --------
                        if (c.Protocol == DeviceProtocol.Modbus)
                        {
                            if (string.IsNullOrWhiteSpace(c.IpAddress))
                                throw new ArgumentException("IpAddress is required for Modbus");

                            if (!c.Port.HasValue || c.Port <= 0 || c.Port > 65535)
                                throw new ArgumentOutOfRangeException(nameof(c.Port));

                            if (!c.SlaveId.HasValue || c.SlaveId < 0 || c.SlaveId > 247)
                                throw new ArgumentOutOfRangeException(nameof(c.SlaveId));
                        }
                        else if (c.Protocol == DeviceProtocol.OpcUa)
                        {
                            if (string.IsNullOrWhiteSpace(c.ConnectionString))
                                throw new ArgumentException(
                                    "ConnectionString is required for OPC UA");

                            if (!c.ConnectionMode.HasValue)
                                throw new ArgumentException(
                                    "ConnectionMode is required for OPC UA");

                            if (c.ConnectionMode == OpcUaConnectionMode.Polling &&
                                !c.PollIntervalMs.HasValue)
                                throw new ArgumentException(
                                    "PollIntervalMs is required for OPC UA Polling");
                        }
                        else
                        {
                            throw new InvalidOperationException("Unsupported protocol");
                        }

                        var cfg = new DeviceConfiguration
                        {
                            ConfigurationId = Guid.NewGuid(),
                            Name = string.IsNullOrWhiteSpace(c.Name)
                                ? $"{device.Name}-cfg"
                                : c.Name.Trim(),

                            Protocol = c.Protocol,

                            // OPC UA
                            ConnectionString = c.Protocol == DeviceProtocol.OpcUa
                                ? c.ConnectionString
                                : null,

                            ConnectionMode = c.Protocol == DeviceProtocol.OpcUa
                                ? c.ConnectionMode
                                : null,

                            // Polling
                            PollIntervalMs = c.Protocol == DeviceProtocol.Modbus
                                ? c.PollIntervalMs ?? 1000
                                : c.ConnectionMode == OpcUaConnectionMode.Polling
                                    ? c.PollIntervalMs
                                    : null,

                            // MODBUS
                            IpAddress = c.Protocol == DeviceProtocol.Modbus ? c.IpAddress : null,
                            Port = c.Protocol == DeviceProtocol.Modbus ? c.Port : null,
                            SlaveId = c.Protocol == DeviceProtocol.Modbus ? c.SlaveId : null,
                            Endian = c.Protocol == DeviceProtocol.Modbus ? c.Endian : null
                        };

                        await _db.DeviceConfigurations.AddAsync(cfg, ct);
                        device.DeviceConfigurationId = cfg.ConfigurationId;
                    }


                    await _db.Devices.AddAsync(device, ct);
                    await _db.SaveChangesAsync(ct);

                    result.CreatedDeviceIds.Add(device.DeviceId);
                    _log.LogInformation("Created device {DeviceId}", device.DeviceId);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Failed to create device {DeviceName}", dto.Name);
                    result.Errors.Add($"Device '{dto.Name}': {ex.Message}");
                }
            }

            await tx.CommitAsync(ct);
            return result;
        }




        public async Task<List<DeviceConfigurationResponseDto>> GetDeviceConfigurationsByGatewayAsync(
        string gatewayId,
        CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(gatewayId))
                throw new ArgumentException(
                    "GatewayId cannot be empty.",
                    nameof(gatewayId));

            // STEP 1: Get devices for this gateway
            var gatewayDeviceIds =
                await _db.Devices.AsNoTracking()
                    .Where(d => d.GatewayId == gatewayId && !d.IsDeleted)
                    .Select(d => d.DeviceId)
                    .ToListAsync(ct);

            if (!gatewayDeviceIds.Any())
                throw new KeyNotFoundException(
                    $"No gateway found with GatewayId '{gatewayId}'.");

            // STEP 2: Get mapping info
            var mappings =
                await _assetDb.MappingTable.AsNoTracking()
                    .Where(m => gatewayDeviceIds.Contains(m.DeviceId))
                    .Select(m => new { m.DeviceId, RegisterId = m.registerId })
                    .ToListAsync(ct);

            if (!mappings.Any())
                return new List<DeviceConfigurationResponseDto>();

            var mappedDeviceIds =
                mappings.Select(m => m.DeviceId).Distinct().ToList();

            // STEP 3: Load devices + configuration + slaves + registers
            var devices =
                await _db.Devices.AsNoTracking()
                    .Where(d => mappedDeviceIds.Contains(d.DeviceId) && !d.IsDeleted)
                    .Include(d => d.DeviceConfiguration)
                    .Include(d => d.DeviceSlave)
                        .ThenInclude(s => s.Registers)
                    .ToListAsync(ct);

            if (!devices.Any())
                return new List<DeviceConfigurationResponseDto>();

            // STEP 4: Build lookup for mapped registers
            var mappedRegistersByDevice =
                mappings.GroupBy(m => m.DeviceId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(x => x.RegisterId).ToHashSet());

            // STEP 5: Build response
            var result =
                devices.Select(device =>
                {
                    var cfg = device.DeviceConfiguration;

                    return new DeviceConfigurationResponseDto
                    {
                        DeviceId = device.DeviceId,
                        Name = device.Name,
                        Protocol = device.Protocol, // device-level protocol (kept)

                        // ---------- Polling ----------
                        PollIntervalMs =
                            cfg == null ? null :
                            cfg.Protocol == DeviceProtocol.Modbus
                                ? cfg.PollIntervalMs ?? 1000
                                : cfg.ConnectionMode == OpcUaConnectionMode.Polling
                                    ? cfg.PollIntervalMs
                                    : null,

                        // ---------- MODBUS ONLY ----------
                        IpAddress =
                            cfg?.Protocol == DeviceProtocol.Modbus
                                ? cfg.IpAddress
                                : null,

                        Port =
                            cfg?.Protocol == DeviceProtocol.Modbus
                                ? cfg.Port
                                : null,

                        SlaveId =
                            cfg?.Protocol == DeviceProtocol.Modbus && cfg.SlaveId.HasValue
                                ? (byte)cfg.SlaveId.Value
                                : (byte?)null,

                        Endian =
                            cfg?.Protocol == DeviceProtocol.Modbus
                                ? cfg.Endian
                                : null,

                        // ---------- OPC UA ----------
                        ConnectionString =
                            cfg?.Protocol == DeviceProtocol.OpcUa
                                ? cfg.ConnectionString
                                : null,

                        ConnectionMode =
                            cfg?.Protocol == DeviceProtocol.OpcUa
                                ? cfg.ConnectionMode
                                : null,

                        Slaves =
                            device.DeviceSlave
                                .Where(s => s.IsHealthy)
                                .Select(s => new SlaveDto
                                {
                                    DeviceSlaveId = s.deviceSlaveId,
                                    SlaveIndex = s.slaveIndex,
                                    IsHealthy = s.IsHealthy,

                                    Registers =
                                        s.Registers
                                            .Where(r =>
                                                r.IsHealthy &&
                                                mappedRegistersByDevice.TryGetValue(
                                                    device.DeviceId,
                                                    out var registerIds) &&
                                                registerIds.Contains(r.RegisterId))
                                            .OrderBy(r => r.RegisterAddress)
                                            .Select(r => new DeviceRegisterDto
                                            {
                                                RegisterId = r.RegisterId,
                                                RegisterAddress = r.RegisterAddress,
                                                RegisterLength = r.RegisterLength,
                                                DataType = r.DataType,
                                                Scale = r.Scale,
                                                Unit = r.Unit,
                                                ByteOrder = r.ByteOrder,
                                                WordSwap = r.WordSwap,
                                                IsHealthy = r.IsHealthy
                                            })
                                            .ToList()
                                })
                                .ToList()
                    };
                })
                .ToList();

            return result;

        }
    }

}
