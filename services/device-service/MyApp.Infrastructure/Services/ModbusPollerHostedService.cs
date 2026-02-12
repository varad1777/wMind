using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyApp.Application.Dtos;
using MyApp.Domain.Entities;
using MyApp.Infrastructure.Data;
using MyApp.Infrastructure.SignalRHub;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MyApp.Infrastructure.Services; 

namespace MyApp.Infrastructure.Services
{
    
    public class ModbusPollerHostedService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<ModbusPollerHostedService> _log;
        private readonly IConfiguration _config;
        private readonly IHubContext<ModbusHub> _hub;
        private readonly RabbitMqService _rabbit;

        private readonly SemaphoreSlim _semaphore;

        private static readonly ConcurrentDictionary<Guid, int> _failureCounts = new();
        private static readonly object _consoleLock = new();

        private readonly ConcurrentDictionary<Guid, Task> _deviceTasks = new();

        private readonly int _failThreshold;

        public IHubContext<ModbusHub> Hub => _hub;

        public ModbusPollerHostedService(IServiceProvider sp, ILogger<ModbusPollerHostedService> log,
                                         IConfiguration config, IHubContext<ModbusHub>? hub, RabbitMqService rabbit)
        {
            _sp = sp;
            _log = log;
            _config = config;
            _hub = hub ?? throw new ArgumentNullException(nameof(hub));

            _failThreshold = config?.GetValue<int?>("Modbus:FailureThreshold") ?? 3;
            if (_failThreshold <= 0) _failThreshold = 3;

            int concurrency = config?.GetValue<int?>("Modbus:MaxConcurrentPolls") ?? 10;
            if (concurrency <= 0) concurrency = 10;
            _semaphore = new SemaphoreSlim(concurrency, concurrency);
            _rabbit = rabbit;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("Modbus poller started (device-per-loop mode)");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var scope = _sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var deviceIds = await db.Devices
                            .AsNoTracking()
                            .Where(d => !d.IsDeleted)
                            .Select(d => d.DeviceId)
                            .ToListAsync(stoppingToken);

                        foreach (var id in deviceIds)
                        {
                            if (_deviceTasks.ContainsKey(id)) continue;

                            var task = Task.Run(() => PollLoopForDeviceAsync(id, stoppingToken), stoppingToken);
                            _deviceTasks.TryAdd(id, task);

                            var completed = _deviceTasks.Where(kvp => kvp.Value.IsCompleted).Select(kvp => kvp.Key).ToList();
                            foreach (var k in completed) _deviceTasks.TryRemove(k, out _);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Poll loop manager error");
                    }

                    // small delay before scanning DB again for new/removed devices
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            finally
            {
                try
                {
                    await Task.WhenAll(_deviceTasks.Values.ToArray());
                }
                catch
                {
                    // ignore exceptions during shutdown
                }
            }
        }

        
        private async Task PollLoopForDeviceAsync(Guid deviceId, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    int delayMs = 1000;
                    try
                    {
                        delayMs = await PollSingleDeviceOnceAsync(deviceId, ct);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, "Unhandled error during single poll for device {Device}", deviceId);
                        delayMs = 1000;
                    }

                    if (delayMs <= 0) delayMs = 1000;
                    await Task.Delay(TimeSpan.FromMilliseconds(delayMs), ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Error in device loop for {Device}", deviceId);
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                }
            }
        }

        
        private async Task<int> PollSingleDeviceOnceAsync(Guid deviceId, CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var device = await db.Devices.Include(d => d.DeviceConfiguration).FirstOrDefaultAsync(d => d.DeviceId == deviceId, ct);
            if (device == null)
            {
                return 1000; // safe default
            }

            if (device.IsDeleted)
            {
                return 1000;
            }

            if (device.DeviceConfigurationId == null)
            {
                return 1000;
            }

            var cfg = device.DeviceConfiguration!;
            
            var ip = cfg.IpAddress;
            var port = cfg.Port ?? 502;
            var slaveIdFromConfig = cfg.SlaveId ?? 1;
            var endian = cfg.Endian ?? "Big";
            var pollIntervalMs = cfg.PollIntervalMs ?? 1000;

            if (string.IsNullOrEmpty(ip))
            {
                return pollIntervalMs;
            }

            var slaves = await db.DeviceSlaves
                .Include(ds => ds.Registers)
                .Where(s => s.DeviceId == device.DeviceId && s.IsHealthy)
                .ToListAsync(ct);

            var activeRegisters = slaves
                .SelectMany(ds => ds.Registers.Where(r => r.IsHealthy).Select(r => new { DeviceSlave = ds, Register = r }))
                .ToList();

            if (!activeRegisters.Any())
            {
                _log.LogWarning("No healthy registers for device {Device}. Ip={Ip} Port={Port}", device.DeviceId, ip, port);
                return pollIntervalMs;
            }

            const int ModbusMaxRegistersPerRead = 125;

            bool dbUses40001 = slaves.Any(s => s.Registers != null && s.Registers.Any(r => r.RegisterAddress >= 40001));

            int ToProto(int dbAddr)
            {
                if (dbUses40001 && dbAddr >= 40001)
                {
                    return dbAddr - 40001;  
                }
                else if (dbAddr > 0 && dbAddr < 40001)
                {
                    return dbAddr - 1;  
                }
                return dbAddr;  
            }

            var protoPorts = activeRegisters.Select(x => new
            {
                DeviceSlave = x.DeviceSlave,
                Register = x.Register,
                ProtoAddr = ToProto(x.Register.RegisterAddress), 
                Length = Math.Max(1, x.Register.RegisterLength)
            })
            .OrderBy(x => x.ProtoAddr)
            .ToList();

            if (!protoPorts.Any())
            {
                _log.LogDebug("No ports after normalization for device {Device}", device.DeviceId);
                return pollIntervalMs;
            }

            await _semaphore.WaitAsync(ct);
            
            try
            {
                using var tcp = new TcpClient();
                using var connectCts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, connectCts.Token);

                await tcp.ConnectAsync(ip, port, linked.Token);

                var now = DateTime.UtcNow;
                var allReads = new List<(Guid deviceSlaveId, int slaveIndex, string SignalType, double Value, string Unit, int RegisterAddress)>();

                var protoGroups = protoPorts
                    .GroupBy(x => ((DeviceSlave)x.DeviceSlave).slaveIndex)
                    .ToDictionary(g => g.Key, g => g.OrderBy(p => p.ProtoAddr).ToList());

                foreach (var kv in protoGroups)
                {
                    int unitId = kv.Key;
                    var itemsForSlave = kv.Value;

                    var slaveRanges = new List<(int Start, int Count, List<dynamic> Items)>();
                    int j = 0;
                    while (j < itemsForSlave.Count)
                    {
                        int start = itemsForSlave[j].ProtoAddr;
                        int end = start + itemsForSlave[j].Length - 1;
                        var items = new List<dynamic> { itemsForSlave[j] };
                        j++;

                        while (j < itemsForSlave.Count)
                        {
                            var next = itemsForSlave[j];
                            if (next.ProtoAddr <= end + 1)
                            {
                                end = Math.Max(end, next.ProtoAddr + next.Length - 1);
                                items.Add(next);
                                j++;
                            }
                            else break;

                            if (end - start + 1 >= ModbusMaxRegistersPerRead)
                            {
                                end = start + ModbusMaxRegistersPerRead - 1;
                                break;
                            }
                        }

                        int count = Math.Min(ModbusMaxRegistersPerRead, end - start + 1);
                        slaveRanges.Add((start, count, items));
                    }

                    foreach (var r in slaveRanges)
                    {
                        if (r.Start < 0 || r.Start > ushort.MaxValue) { _log.LogWarning("Skipping invalid start {Start}", r.Start); continue; }
                        if (r.Count <= 0) continue;

                        StringBuilder sb = new();
                        sb.AppendLine();
                        sb.AppendLine(new string('=', 80));
                        sb.AppendLine($"Device: {device.DeviceId} | Ip={ip}:{port} | UnitId={unitId} | RangeStart={r.Start} Count={r.Count}");
                        sb.AppendLine(new string('-', 80));
                        sb.AppendLine("Included registers:");
                        foreach (var ent in r.Items)
                        {
                            var ds = (DeviceSlave)ent.DeviceSlave;
                            var reg = (Register)ent.Register;
                            sb.AppendLine($"  - slaveIndex={ds.slaveIndex}, DBAddr={reg.RegisterAddress}, Length={ent.Length}, DataType={reg.DataType}");
                        }
                        sb.AppendLine();

                        try
                        {
                            ushort[] regs = await ModbusTcpClient.ReadHoldingRegistersAsync(tcp, (byte)unitId, (ushort)r.Start, (ushort)r.Count, ct);
                            sb.AppendLine($"Read {regs.Length} registers from unit={unitId} start={r.Start}");
                            sb.AppendLine(new string('-', 80));

                            foreach (var ent in r.Items)
                            {
                                var reg = (Register)ent.Register;
                                _failureCounts.TryRemove(reg.RegisterId, out _);
                            }

                            sb.AppendLine($"{"Time (UTC)".PadRight(30)} | {"Unit".PadRight(6)} | {"Register".PadRight(8)} | {"Value".PadRight(15)} | {"Unit".PadRight(8)}");
                            sb.AppendLine(new string('-', 80));

                            foreach (var entry in r.Items)
                            {
                                var ds = (DeviceSlave)entry.DeviceSlave;
                                var reg = (Register)entry.Register;
                                int protoAddr = entry.ProtoAddr;
                                int relativeIndex = protoAddr - r.Start;

                                if (relativeIndex < 0 || relativeIndex + (entry.Length - 1) >= regs.Length)
                                {
                                    sb.AppendLine($"Index out-of-range for slave {ds.slaveIndex} (proto {protoAddr})");
                                    continue;
                                }

                                double finalValue = 0.0;
                                try
                                {
                                    if (string.Equals(reg.DataType, "float32", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (relativeIndex + 1 >= regs.Length)
                                        {
                                            sb.AppendLine($"Not enough regs to decode float32 for slave {ds.slaveIndex}");
                                            continue;
                                        }

                                        ushort r1 = regs[relativeIndex];
                                        ushort r2 = regs[relativeIndex + 1];
                                        byte[] bytes = new byte[4];
                                        bool wordSwap = reg.WordSwap;

                                        if (wordSwap)
                                        {
                                            bytes[0] = (byte)(r2 >> 8);
                                            bytes[1] = (byte)(r2 & 0xFF);
                                            bytes[2] = (byte)(r1 >> 8);
                                            bytes[3] = (byte)(r1 & 0xFF);
                                        }
                                        else
                                        {
                                            bytes[0] = (byte)(r1 >> 8);
                                            bytes[1] = (byte)(r1 & 0xFF);
                                            bytes[2] = (byte)(r2 >> 8);
                                            bytes[3] = (byte)(r2 & 0xFF);
                                        }

                                        if (string.Equals(endian, "Little", StringComparison.OrdinalIgnoreCase))
                                            Array.Reverse(bytes);

                                        float raw = BitConverter.ToSingle(bytes, 0);

                                        if (float.IsNaN(raw) || float.IsInfinity(raw) || Math.Abs(raw) > 1e6)
                                        {
                                            sb.AppendLine($"Detected invalid float for slave {ds.slaveIndex}, using fallback/zero.");
                                            raw = 0;
                                        }

                                        if ((r1 == 0 && r2 == 0) || Math.Abs(raw) < 1e-3)
                                        {
                                            double scaledFallback = r1;
                                            finalValue = scaledFallback * reg.Scale;
                                            sb.AppendLine($"Float32 fallback for slave {ds.slaveIndex}: r1={r1}, r2={r2}, scaled={scaledFallback}");
                                        }
                                        else
                                        {
                                            finalValue = raw * reg.Scale;
                                        }
                                    }
                                    else
                                    {
                                        finalValue = regs[relativeIndex] * reg.Scale;
                                    }

                                    allReads.Add((ds.deviceSlaveId, ds.slaveIndex, reg.DataType ?? $"Port{ds.slaveIndex}", finalValue, reg.Unit ?? string.Empty, reg.RegisterAddress));

                                    sb.AppendLine($"{now:O}".PadRight(30) + " | " +
                                                  ds.slaveIndex.ToString().PadRight(6) + " | " +
                                                  reg.RegisterAddress.ToString().PadRight(8) + " | " +
                                                  finalValue.ToString("G6").PadRight(15) + " | " +
                                                  (reg.Unit ?? string.Empty).PadRight(8));
                                }
                                catch (Exception decodeEx)
                                {
                                    sb.AppendLine($"Decode failed for slave {ds.slaveIndex}: {decodeEx.Message}");
                                }
                            }

                            sb.AppendLine(new string('=', 80));
                            lock (_consoleLock)
                            {
                                Console.Write(sb.ToString());
                            }
                        }
                        catch (Modbus.SlaveException sex)
                        {
                            var regsToConsider = r.Items.Select(it => ((Register)it.Register).RegisterId).ToList();
                            try
                            {
                                var dbRegs = await db.Registers.Where(reg => regsToConsider.Contains(reg.RegisterId)).ToListAsync(ct);
                                var toMark = new List<Guid>();

                                foreach (var reg in dbRegs)
                                {
                                    var id = reg.RegisterId;
                                    int newCount = _failureCounts.AddOrUpdate(id, 1, (_, old) => old + 1);
                                    if (newCount >= _failThreshold && reg.IsHealthy) toMark.Add(id);
                                }

                                if (toMark.Any())
                                {
                                    var markRegs = await db.Registers.Where(reg => toMark.Contains(reg.RegisterId)).ToListAsync(ct);
                                    foreach (var mr in markRegs) mr.IsHealthy = false;
                                    await db.SaveChangesAsync(ct);
                                }
                            }
                            catch (Exception markEx)
                            {
                                _log.LogError(markEx, "Failed marking unhealthy registers for device {Device}", device.DeviceId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogError(ex, "Error reading device {Device} unit={UnitId} start={Start} count={Count}", device.DeviceId, unitId, r.Start, r.Count);
                        }
                    }
                }

                if (allReads.Count > 0)
                {
                    try
                    {
                        Dictionary<string, Guid>? signalLookup = null;
                        
                        try
                        {
                            using var lookupScope = _sp.CreateScope();
                            var signalLookupService = lookupScope.ServiceProvider.GetRequiredService<ISignalLookupService>();
                            signalLookup = await signalLookupService.GetSignalLookupForDeviceAsync(device.DeviceId, ct);
                            
                            if (signalLookup != null && signalLookup.Any())
                            {
                                _log.LogDebug("Loaded {Count} signal mappings for device {Device}", 
                                    signalLookup.Count, device.DeviceId);
                            }
                            else
                            {
                                _log.LogDebug(" No signal mappings found for device {Device}", device.DeviceId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.LogWarning(ex, " Could not load signal mappings for device {Device}", device.DeviceId);
                        }

                        var signalTelemetryList = new List<SignalTelemetryDto>();
                        int unmappedCount = 0;

                        foreach (var r in allReads)
                        {
                            string lookupKey = $"{device.DeviceId}_{r.RegisterAddress}";

                            if (signalLookup != null && signalLookup.TryGetValue(lookupKey, out Guid signalId))
                            {
                                var signalDto = new SignalTelemetryDto(
                                    SignalId: signalId,
                                    Value: r.Value,
                                    Timestamp: now
                                );
                                signalTelemetryList.Add(signalDto);
                            }
                            else
                            {
                                unmappedCount++;
                            }
                        }

                        if (unmappedCount > 0)
                        {
                            _log.LogDebug("  {UnmappedCount} out of {TotalCount} registers have no signal mapping for device {Device}", 
                                unmappedCount, allReads.Count, device.DeviceId);
                        }

                        if (signalTelemetryList.Any())
                        {
                            try
                            {
                                foreach (var dto in signalTelemetryList)
                                {
                                    Console.WriteLine($" Queue → SignalId: {dto.SignalId}, Value: {dto.Value}, Time: {dto.Timestamp:HH:mm:ss}");
                                    ct.ThrowIfCancellationRequested();
                                    await _rabbit.PublishAsync(dto, ct);
                                }
                                
                                _log.LogInformation(" Published {Count} signal telemetry to RabbitMQ", signalTelemetryList.Count);
                            }
                            catch (Exception rmqEx)
                            {
                                _log.LogError(rmqEx, " Failed to publish to RabbitMQ for device {Device}", device.DeviceId);
                            }
                        }

                        var telemetryDtos = allReads.Select(r => new TelemetryDto(
                            DeviceId: device.DeviceId,
                            deviceSlaveId: r.deviceSlaveId,
                            slaveIndex: r.slaveIndex,
                            RegisterAddress: r.RegisterAddress,
                            SignalType: r.SignalType,
                            Value: r.Value,
                            Unit: r.Unit,
                            Timestamp: now
                        )).ToList();

                        if (telemetryDtos.Any())
                        {
                            try
                            {
                                await _hub.Clients.Group(device.DeviceId.ToString())
                                    .SendAsync("TelemetryUpdate", telemetryDtos, ct);
                            }
                            catch (Exception hubEx)
                            {
                                _log.LogWarning(hubEx, " Failed to push to SignalR for device {Device}", device.DeviceId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, " Failed to process telemetry for device {Device}", device.DeviceId);
                    }
                }
            }
            catch (SocketException s_ex)
            {
                // Device unreachable
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Polling cancelled
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error polling device {Device}", device.DeviceId);
            }
            finally
            {
                _semaphore.Release();
            }

            return pollIntervalMs;
        }
    }
}