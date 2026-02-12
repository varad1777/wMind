using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApp.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyApp.Infrastructure.Services
{
    public interface ISignalLookupService
    {
        Task<Dictionary<string, Guid>> GetSignalLookupForDeviceAsync(Guid deviceId, CancellationToken ct);
    }

    public class SignalLookupService : ISignalLookupService
    {
        private readonly AssetDbContextForDevice _assetDb;
        private readonly ILogger<SignalLookupService> _log;

        public SignalLookupService(AssetDbContextForDevice assetDb, ILogger<SignalLookupService> log)
        {
            _assetDb = assetDb;
            _log = log;
        }

        public async Task<Dictionary<string, Guid>> GetSignalLookupForDeviceAsync(Guid deviceId, CancellationToken ct)
        {
            try
            {
                var lookup = await (
                    from mapping in _assetDb.MappingTable
                    join signal in _assetDb.Signals
                        on new { mapping.DeviceId, mapping.AssetId, mapping.SignalName }
                        equals new { signal.DeviceId, signal.AssetId, signal.SignalName }
                    where mapping.DeviceId == deviceId
                    select new
                    {
                        // ⚠️  Using RegisterAdress (one 'd') to match entity typo
                        Key = $"{mapping.DeviceId}_{mapping.RegisterAdress}",
                        SignalId = signal.SignalId
                    }
                ).ToDictionaryAsync(x => x.Key, x => x.SignalId, ct);

                _log.LogDebug("Built signal lookup for device {DeviceId}: {Count} mappings", deviceId, lookup.Count);
                
                // 🔍 Optional: Log sample mappings for debugging
                if (lookup.Any())
                {
                    var sample = lookup.Take(3);
                    foreach (var kvp in sample)
                    {
                        _log.LogDebug("  Sample mapping: {Key} → {SignalId}", kvp.Key, kvp.Value);
                    }
                }
                
                return lookup;
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Failed to build signal lookup for device {DeviceId}", deviceId);
                return new Dictionary<string, Guid>();
            }
        }
    }
}