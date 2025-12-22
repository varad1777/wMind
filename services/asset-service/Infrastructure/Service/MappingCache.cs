using Application.Interface;

using Infrastructure.DBs;
using MappingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;

namespace Infrastructure.Service
{
    public class MappingCache : IMappingCache, IDisposable
    {
        private readonly IDbContextFactory<DBContext> _dbFactory;
        private readonly TimeSpan _refreshInterval;//cache refresh ka interval
        private readonly CancellationTokenSource _cts = new();
        private volatile ConcurrentDictionary<(Guid deviceId, Guid devicePortId ,int RegisterAdress), MappingInfo> _cache//actual in memeory cache
            = new();

        public MappingCache(IDbContextFactory<DBContext> dbFactory, TimeSpan? refreshInterval = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _refreshInterval = refreshInterval ?? TimeSpan.FromSeconds(5);
            _ = Task.Run(() => RefreshLoopAsync(_cts.Token));
        }

        public bool TryGet(Guid deviceId, Guid devicePortId,int RegisterAdress, out MappingInfo mapping)
            => _cache.TryGetValue((deviceId, devicePortId, RegisterAdress), out mapping);//read the cache

        public async Task RefreshAsync(CancellationToken ct = default)
        {
            using var db = _dbFactory.CreateDbContext();
            var rows = await db.MappingTable.AsNoTracking()
                .Select(m => new
                {
                    m.MappingId,
                    m.DeviceId,
                    m.DevicePortId,
                    m.AssetId,
                    m.SignalTypeId,
                    m.SignalName,
                    m.SignalUnit,
                    RegisterAddress = m.RegisterAdress
                })
                .ToListAsync(ct);

            var newCache = new ConcurrentDictionary<(Guid, Guid, int), MappingInfo>();
            foreach (var r in rows)
            {
                newCache[(r.DeviceId, r.DevicePortId,r.RegisterAddress)] = new MappingInfo
                {
                    MappingId=r.MappingId,
                    AssetId = r.AssetId,
                    SignalTypeId = r.SignalTypeId,
                    SignalName = r.SignalName,
                    SignalUnit = r.SignalUnit,
                    DeviceId=r.DeviceId,
                    DeviceSlaveID=r.DevicePortId,
                    RegisterAddress = r.RegisterAddress
                };
            }

            _cache = newCache;
        }

        private async Task RefreshLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await RefreshAsync(ct); }
                catch { /* TODO: log */ }

                try { await Task.Delay(_refreshInterval, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
