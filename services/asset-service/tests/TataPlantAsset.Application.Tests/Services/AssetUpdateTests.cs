using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using Xunit;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetUpdateTests
    {
        private readonly DbContextFixture _fixture;

        public AssetUpdateTests()
        {
            _fixture = new DbContextFixture();
        }

        [Fact]
        public async Task UpdateAssetName_Should_Work()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<AssetHierarchyService>();
            var service = new AssetHierarchyService(logger, context);

            var insertDto = new InsertionAssetDto { Name = "OriginalName" };
            await service.InsertAssetAsync(insertDto);

            var asset = await context.Assets.FirstAsync(a => a.Name == "OriginalName");

            var updateDto = new UpdateAssetDto
            {
                AssetId = asset.AssetId,
                NewName = "UpdatedName"
            };

            // Act
            var (isUpdated, message) = await service.UpdateAssetName(updateDto);

            // Assert
            isUpdated.Should().BeTrue();
            var updatedAsset = await context.Assets.FindAsync(asset.AssetId);
            updatedAsset.Name.Should().Be("UpdatedName");
        }
    }
}
