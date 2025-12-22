using Application.Interface;
using Domain.Entities;
using Infrastructure.DBs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Service
{
    public class AlertRepository : IAlertRepository
    {
        private readonly DBContext _db;

        public AlertRepository(DBContext db)
        {
            _db = db;
        }

        public async Task<Alert?> GetActiveAsync(Guid mappingId)
        {
            return await _db.Alerts
                .Where(x => x.MappingId == mappingId && x.IsActive)
                .OrderByDescending(x => x.AlertStartUtc)
                .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Alert alert)
        {
            _db.Alerts.Add(alert);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateStatsAsync(Guid alertId, double value)
        {
            var alert = await GetByIdAsync(alertId);
            if (alert == null) return;

            alert.MinObservedValue =
                Math.Min(alert.MinObservedValue ?? value, value);

            alert.MaxObservedValue =
                Math.Max(alert.MaxObservedValue ?? value, value);

            alert.UpdatedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task ResolveAsync(Guid alertId, DateTime resolvedUtc)
        {
            var alert = await GetByIdAsync(alertId);
            if (alert == null) return;

            alert.IsActive = false;
            alert.AlertEndUtc = resolvedUtc;
            alert.UpdatedUtc = resolvedUtc;

            await _db.SaveChangesAsync();
        }


        public async Task<List<Alert>> GetUnAnalyzedByAssetAsync(
     Guid assetId,
     DateTime fromUtc,
     DateTime toUtc)
        {
            return await _db.Alerts
                .Where(a =>
                    a.AssetId == assetId &&
                    !a.IsAnalyzed &&
                    !a.IsActive &&
                    a.AlertStartUtc >= fromUtc &&
                    a.AlertEndUtc <= toUtc)
                .ToListAsync();
        }

        public async Task<List<Alert>> GetUnAnalyzedByAssetIDAsync(
   Guid assetId)
        {
            return await _db.Alerts
                .Where(a =>
                    a.AssetId == assetId &&
                    !a.IsAnalyzed &&
                    !a.IsActive)
                .ToListAsync();
        }


        public Task<Alert?> GetByIdAsync(Guid alertId)
        {
            return _db.Alerts.FirstOrDefaultAsync(a => a.AlertId == alertId);
        }

        public async Task MarkResolvedAsync(Guid alertId, DateTime resolvedAt)
        {
            var alert = await GetByIdAsync(alertId);
            if (alert == null) return;

            alert.IsActive = false;
            alert.AlertEndUtc = resolvedAt;
            alert.UpdatedUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }

        public async Task MarkAnalyzedAsync(IEnumerable<Guid> alertIds)
        {
            await _db.Alerts
                .Where(a => alertIds.Contains(a.AlertId))
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(a => a.IsAnalyzed, true)
                     .SetProperty(a => a.UpdatedUtc, DateTime.UtcNow));
        }

        public async Task<List<Alert>> GetAllAsync(
      DateTime? fromUtc,
      DateTime? toUtc,
      Guid assetId)
        {
            var query = _db.Alerts.AsQueryable();

            // Filter by AssetId
            if (assetId != Guid.Empty)
            {
                query = query.Where(x => x.AssetId == assetId);
            }

            // Filter by start time
            if (fromUtc.HasValue)
            {
                query = query.Where(x => x.AlertStartUtc >= fromUtc.Value);
            }

            // Filter by end time
            if (toUtc.HasValue)
            {
                query = query.Where(x => x.AlertStartUtc <= toUtc.Value);
            }

            return await query
                .OrderByDescending(x => x.AlertStartUtc)
                .ToListAsync();
        }



    }
}
