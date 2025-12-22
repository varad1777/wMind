using Application.DTOs;
using FluentAssertions;
using Infrastructure.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TataPlantAsset.Application.Tests.Fixtures;
using TataPlantAsset.Application.Tests.TestData;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace TataPlantAsset.Application.Tests.Services
{
    public class AssetInsertTests : IClassFixture<DbContextFixture>
    {
        private readonly DbContextFixture _fixture;
        private readonly ILogger<AssetHierarchyService> _logger;

        public AssetInsertTests(DbContextFixture fixture)
        {
            _fixture = fixture;
            _logger = new LoggerFactory().CreateLogger<AssetHierarchyService>();
        }

        [Fact]
        public async Task InsertAsset_Should_Work()
        {
            // Arrange
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_logger, context);
            var dto = AssetTestData.RootAsset;

            // Act
            var result = await service.InsertAssetAsync(dto);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_LevelExceeds5()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_logger, context);

            // Create parent hierarchy up to level 5
            Guid parentId = Guid.NewGuid();
            for (int i = 1; i <= 5; i++)
            {
                await context.Assets.AddAsync(new Asset { AssetId = parentId, Name = $"Level{i}", Level = i });
                parentId = Guid.NewGuid();
            }
            await context.SaveChangesAsync();

            var dto = new InsertionAssetDto { Name = "TooDeep", ParentId = parentId };

            await Assert.ThrowsAsync<Exception>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_NameTooShort()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_logger, context);

            var dto = new InsertionAssetDto { Name = "A" }; // Too short

            await Assert.ThrowsAsync<DbUpdateException>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_NameContainsInvalidCharacters()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_logger, context);

            var dto = new InsertionAssetDto { Name = "Invalid@Name" }; // Invalid characters

            await Assert.ThrowsAsync<DbUpdateException>(() => service.InsertAssetAsync(dto));
        }

        [Fact]
        public async Task InsertAsset_Should_Throw_When_DuplicateNameUnderSameParent()
        {
            var context = _fixture.CreateContext();
            var service = new AssetHierarchyService(_logger, context);

            var parentDto = AssetTestData.RootAsset;
            await service.InsertAssetAsync(parentDto);

            var duplicateDto = new InsertionAssetDto
            {
                Name = parentDto.Name, // same name
                ParentId = parentDto.ParentId
            };

            await Assert.ThrowsAsync<DbUpdateException>(() => service.InsertAssetAsync(duplicateDto));
        }
    }
}
