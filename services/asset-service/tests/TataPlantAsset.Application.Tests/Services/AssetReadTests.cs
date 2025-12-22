using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using Xunit;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetReadTests
    {
        private readonly DbContextFixture _fixture;

        public AssetReadTests()
        {
            _fixture = new DbContextFixture();
        }

        [Fact]
        public async Task GetByParentId_Should_Return_Assets()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<AssetHierarchyService>();
            var service = new AssetHierarchyService(logger, context);

            // Insert parent and child
            var parentDto = new InsertionAssetDto { Name = "ParentAsset" };
            await service.InsertAssetAsync(parentDto);
            var parent = await context.Assets.FirstAsync(a => a.Name == "ParentAsset");

            var childDto = new InsertionAssetDto { Name = "ChildAsset", ParentId = parent.AssetId };
            await service.InsertAssetAsync(childDto);

            // Act
            var children = await service.GetByParentIdAsync(parent.AssetId);

            // Assert
            children.Should().HaveCount(1);
            children.First().Name.Should().Be("ChildAsset");
        }

        [Fact]
        public async Task GetAssetHierarchy_Should_Return_Roots()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var logger = new LoggerFactory().CreateLogger<AssetHierarchyService>();
            var service = new AssetHierarchyService(logger, context);

            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root1" });
            await service.InsertAssetAsync(new InsertionAssetDto { Name = "Root2" });

            // Act
            var roots = await service.GetAssetHierarchy();

            // Assert
            roots.Should().HaveCount(2);
        }
    }
}
