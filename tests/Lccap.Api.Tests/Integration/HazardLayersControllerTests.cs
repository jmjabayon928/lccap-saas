using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.HazardLayers.Commands;
using Lccap.Application.HazardLayers.Dtos;
using Lccap.Application.HazardLayers.Queries;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Lccap.Api.Tests.Integration;

public sealed class HazardLayersControllerTests
{
    [Fact]
    public async Task Register_hazard_layer_creates_record_for_same_tenant_geojson_hazard_map_asset()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Map plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var mapAsset = NewMapAsset(accountId, plan.Id, file.Id, "Hazard");
        db.MapAssets.Add(mapAsset);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var command = new RegisterHazardLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.RegisterHazardLayer(
            plan.Id,
            new RegisterHazardLayerRequest(mapAsset.Id, "Haz 1", "River", "High", "Source A", null),
            command,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var dto = Assert.IsType<HazardLayerDto>(created.Value);

        Assert.Equal(mapAsset.Id, dto.MapAssetId);
        Assert.Equal(plan.Id, dto.PlanId);

        var dbRow = await db.HazardLayers.SingleAsync(h => !h.IsDeleted && h.MapAssetId == mapAsset.Id);
        Assert.Equal(accountId, dbRow.AccountId);
        Assert.Equal(plan.Id, dbRow.PlanId);
        Assert.Equal(mapAsset.Id, dbRow.MapAssetId);
        Assert.True(dbRow.IsActive);
        Assert.False(dbRow.IsDeleted);
    }

    [Fact]
    public async Task Register_hazard_layer_rejects_cross_tenant_plan_or_map_asset_without_leaking_existence()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var planOtherTenant = await SeedPlan(db, otherAccountId, "Other plan");

        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var mapAsset = NewMapAsset(accountId, planOtherTenant.Id, file.Id, "Hazard");
        db.MapAssets.Add(mapAsset);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var command = new RegisterHazardLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        // Cross-tenant plan: should not reveal existence
        var result1 = await controller.RegisterHazardLayer(
            planOtherTenant.Id,
            new RegisterHazardLayerRequest(mapAsset.Id, "Haz 1", "River", "High", null, null),
            command,
            CancellationToken.None);
        Assert.IsType<NotFoundResult>(result1);

        // Cross-tenant map asset: correct plan for requester, wrong map asset for tenant
        var plan = await SeedPlan(db, accountId, "Plan");
        var mapAssetOtherTenant = NewMapAsset(otherAccountId, plan.Id, file.Id, "Hazard");
        mapAssetOtherTenant.FileAssetId = file.Id;
        db.MapAssets.Add(mapAssetOtherTenant);
        await db.SaveChangesAsync();

        var result2 = await controller.RegisterHazardLayer(
            plan.Id,
            new RegisterHazardLayerRequest(mapAssetOtherTenant.Id, "Haz 1", "River", "High", null, null),
            command,
            CancellationToken.None);
        Assert.IsType<NotFoundResult>(result2);

        Assert.Empty(db.HazardLayers);
    }

    [Fact]
    public async Task Register_hazard_layer_rejects_non_hazard_map_asset()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var nonHazard = NewMapAsset(accountId, plan.Id, file.Id, "Flood");
        db.MapAssets.Add(nonHazard);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var command = new RegisterHazardLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.RegisterHazardLayer(
            plan.Id,
            new RegisterHazardLayerRequest(nonHazard.Id, "Haz 1", "River", "High", null, null),
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(db.HazardLayers);
    }

    [Fact]
    public async Task Register_hazard_layer_rejects_non_geojson_map_asset()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var mapAsset = NewMapAsset(accountId, plan.Id, file.Id, "Hazard");
        mapAsset.MapFormat = "Pdf";
        db.MapAssets.Add(mapAsset);
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var command = new RegisterHazardLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.RegisterHazardLayer(
            plan.Id,
            new RegisterHazardLayerRequest(mapAsset.Id, "Haz 1", "River", "High", null, null),
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(db.HazardLayers);
    }

    [Fact]
    public async Task Register_hazard_layer_rejects_duplicate_active_layer_for_same_map_asset()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var mapAsset = NewMapAsset(accountId, plan.Id, file.Id, "Hazard");
        db.MapAssets.Add(mapAsset);
        await db.SaveChangesAsync();

        db.HazardLayers.Add(new HazardLayer
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            MapAssetId = mapAsset.Id,
            Name = "Existing",
            HazardType = "River",
            Severity = "High",
            Source = null,
            Description = null,
            GeometryId = null,
            MetadataJson = JsonDocument.Parse("{}"),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var command = new RegisterHazardLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.RegisterHazardLayer(
            plan.Id,
            new RegisterHazardLayerRequest(mapAsset.Id, "Haz 1", "River", "High", null, null),
            command,
            CancellationToken.None);

        Assert.IsType<ConflictResult>(result);
        Assert.Single(db.HazardLayers.Where(h => !h.IsDeleted));
    }

    [Fact]
    public async Task Get_hazard_layers_returns_only_current_tenant_non_deleted_layers()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var otherPlan = await SeedPlan(db, otherAccountId, "Other");

        db.HazardLayers.AddRange(
            new HazardLayer
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = plan.Id,
                MapAssetId = null,
                Name = "A",
                HazardType = "River",
                Severity = "High",
                Source = null,
                Description = null,
                GeometryId = null,
                MetadataJson = JsonDocument.Parse("{}"),
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                IsDeleted = false,
                DeletedAtUtc = null,
                DeletedByUserId = null,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
            },
            new HazardLayer
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = plan.Id,
                MapAssetId = null,
                Name = "Deleted",
                HazardType = "River",
                Severity = "High",
                Source = null,
                Description = null,
                GeometryId = null,
                MetadataJson = JsonDocument.Parse("{}"),
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                IsDeleted = true,
                DeletedAtUtc = DateTimeOffset.UtcNow,
                DeletedByUserId = userId,
                RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 }
            },
            new HazardLayer
            {
                Id = Guid.NewGuid(),
                AccountId = otherAccountId,
                PlanId = otherPlan.Id,
                MapAssetId = null,
                Name = "Other tenant",
                HazardType = "River",
                Severity = "High",
                Source = null,
                Description = null,
                GeometryId = null,
                MetadataJson = JsonDocument.Parse("{}"),
                IsActive = true,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
                UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-3),
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                IsDeleted = false,
                DeletedAtUtc = null,
                DeletedByUserId = null,
                RowVersion = new byte[] { 3, 2, 3, 4, 5, 6, 7, 8 }
            });
        await db.SaveChangesAsync();

        var controller = CreateController(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var query = new GetPlanHazardLayersQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetPlanHazardLayers(plan.Id, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        var wrapper = JsonSerializer.Deserialize<ItemsResponse<HazardLayerDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(wrapper);
        Assert.Single(wrapper!.items);
        Assert.Equal("A", wrapper.items[0].Name);
    }

    private static HazardLayersController CreateController(LccapDbContext db, ICurrentUserContext context)
    {
        _ = db;
        return new HazardLayersController(context);
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"hazard-layers-tests-{Guid.NewGuid()}")
            .Options;

        return new TestLccapDbContext(options);
    }

    private sealed record ItemsResponse<T>(IReadOnlyList<T> items);

    private static async Task<Plan> SeedPlan(LccapDbContext db, Guid accountId, string title, bool isDeleted = false)
    {
        var plan = new Plan
        {
            AccountId = accountId,
            Title = title,
            StartYear = 2025,
            EndYear = 2026,
            Status = "Draft",
            TemplateMode = "New",
            VersionNumber = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTimeOffset.UtcNow : null,
            DeletedByUserId = isDeleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        _ = db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static FileAsset SeedGeoJsonFileAsset(Guid accountId) =>
        new()
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            OwnerType = "GeoJsonAttachment",
            OwnerId = null,
            OriginalFileName = "layer.geojson",
            StoredFileName = "layer.bin",
            StoredPath = "internal/geo-path",
            ContentType = "application/geo+json",
            FileExtension = ".geojson",
            FileSizeBytes = 120,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 16, 2, 3, 4, 5, 6, 7, 8 },
        };

    private static MapAsset NewMapAsset(Guid accountId, Guid planId, Guid fileId, string mapType)
    {
        var now = DateTimeOffset.UtcNow;
        return new MapAsset
        {
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = fileId,
            Name = "Layer",
            MapType = mapType,
            MapFormat = "GeoJson",
            DefaultStyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 13, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(Guid? accountId, Guid? userId, bool isAuthenticated, string? role = null)
        {
            AccountId = accountId;
            UserId = userId;
            IsAuthenticated = isAuthenticated;
            Role = role;
        }

        public Guid? UserId { get; }

        public Guid? AccountId { get; }

        public string? Role { get; }

        public bool IsAuthenticated { get; }
    }

    private sealed class TestLccapDbContext : LccapDbContext
    {
        public TestLccapDbContext(DbContextOptions<LccapDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var jsonConverter = new ValueConverter<JsonDocument?, string?>(
                value => value == null ? null : value.RootElement.GetRawText(),
                value => value == null ? null : JsonDocument.Parse(value, new JsonDocumentOptions()));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties().Where(p => p.ClrType == typeof(JsonDocument)))
                {
                    property.SetValueConverter(jsonConverter);
                }
            }
        }
    }
}

