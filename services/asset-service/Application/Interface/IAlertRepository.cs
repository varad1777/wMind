using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interface
{
    public interface IAlertRepository
    {
        Task<Alert?> GetActiveAsync(Guid mappingId);

        Task CreateAsync(Alert alert);

        Task UpdateStatsAsync(Guid alertId, double value);

        Task ResolveAsync(Guid alertId, DateTime resolvedUtc);

        Task<Alert?> GetByIdAsync(Guid alertId);
        Task<List<Alert>> GetUnAnalyzedByAssetAsync(Guid assetId, DateTime fromUtc, DateTime toUtc);
        Task<List<Alert>> GetUnAnalyzedByAssetIDAsync(Guid assetId);
        Task MarkAnalyzedAsync(IEnumerable<Guid> alertIds);
        Task MarkResolvedAsync(Guid alertId, DateTime resolvedAt);
        Task<List<Alert>> GetAllAsync(DateTime? fromUtc, DateTime? toUtc , Guid assetId);
    }
}
