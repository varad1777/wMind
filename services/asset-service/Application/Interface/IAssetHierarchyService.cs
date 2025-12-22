using Application.DTOs;
using Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Interface
{
    public interface IAssetHierarchyService
    {
        Task<List<Asset>> GetAssetHierarchy();
        Task<List<AssetDto>> GetByParentIdAsync(Guid? parentId);
        Task<bool> InsertAssetAsync(InsertionAssetDto dto);

        Task<(bool Success, string Message)> UpdateAssetName(UpdateAssetDto dto);

        Task<bool> DeleteAsset(Guid AssetId);
        Task<List<AssetDto>> SearchAssetsAsync(string? searchTerm);

        Task<List<AssetDto>> GetDeletedAssetsAsync();

        Task<bool> RestoreAssetAsync(Guid assetId);
        Task<string?> GetAssetNameAsync(Guid assetId);
        Task<SignalTypes?> GetSignalTypeAsync(Guid signalTypeId);

        Task<AssetUploadResponse> BulkInsertAssetsAsync(AssetUploadRequest assets);

    }
}