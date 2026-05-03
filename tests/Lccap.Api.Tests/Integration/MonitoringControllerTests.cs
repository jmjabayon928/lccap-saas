using System.Text.Json;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring.Commands;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class MonitoringControllerTests
{
    [Fact]
    public async Task CreateIndicator_WithValidAccountAndPlan_Succeeds()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var command = new CreateIndicatorCommand();

        var result = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(
                planId,
                null,
                "Households reached",
                "desc",
                1m,
                10m,
                "count",
                "NotStarted",
                "{}"),
            command,
            dbContext,
            currentUser,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode ?? StatusCodes.Status200OK);
        Assert.Equal(1, await dbContext.MonitoringIndicators.CountAsync());
        var body = Assert.IsType<MonitoringController.IndicatorResponse>(ok.Value);
        Assert.Equal("Households reached", body.Name);
        Assert.False(string.IsNullOrEmpty(body.RowVersion));
    }

    [Fact]
    public async Task CreateIndicator_SpoofedAccountInBodyCannotCrossTenant_ReturnsNotFound()
    {
        var ownerAccountId = Guid.NewGuid();
        var callerAccountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, ownerAccountId, planId);

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = callerAccountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var command = new CreateIndicatorCommand();

        var result = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(
                planId,
                null,
                "Indicator",
                null,
                null,
                null,
                null,
                "NotStarted",
                "{}"),
            command,
            dbContext,
            currentUser,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(0, await dbContext.MonitoringIndicators.CountAsync());
    }

    [Fact]
    public async Task Update_monitoring_indicator_updates_allowed_fields_and_returns_updated_indicator()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);
        dbContext.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = accountId,
            PlanId = planId,
            Name = "Before",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var indicator = await dbContext.MonitoringIndicators.FirstAsync();
        var currentVersion = Convert.ToBase64String(indicator.RowVersion);
        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var updateCommand = new UpdateMonitoringIndicatorCommand(dbContext, currentUser);

        var result = await controller.UpdateIndicator(
            indicatorId,
            new MonitoringController.UpdateIndicatorRequest(
                "After",
                "new",
                1m,
                2m,
                "pct",
                "OnTrack",
                null,
                null,
                null,
                null,
                RowVersion: currentVersion),
            updateCommand,
            currentUser,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<MonitoringController.IndicatorResponse>(ok.Value);
        Assert.Equal("After", body.Name);
        Assert.Equal("OnTrack", body.Status);
        var updated = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal("After", updated.Name);
    }

    [Fact]
    public async Task GetIndicators_ReturnsOnlyCurrentAccountAndPlan()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var plan1 = Guid.NewGuid();
        var plan2 = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountA, plan1);
        await SeedPlanAsync(dbContext, accountA, plan2);
        await SeedPlanAsync(dbContext, accountB, Guid.NewGuid());

        dbContext.MonitoringIndicators.AddRange(
            new MonitoringIndicator
            {
                Id = Guid.NewGuid(),
                AccountId = accountA,
                PlanId = plan1,
                Name = "Included",
                Status = "NotStarted",
                MetadataJson = "{}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 1 }
            },
            new MonitoringIndicator
            {
                Id = Guid.NewGuid(),
                AccountId = accountA,
                PlanId = plan2,
                Name = "Excluded by plan",
                Status = "NotStarted",
                MetadataJson = "{}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 2 }
            },
            new MonitoringIndicator
            {
                Id = Guid.NewGuid(),
                AccountId = accountB,
                PlanId = plan1,
                Name = "Excluded by account",
                Status = "NotStarted",
                MetadataJson = "{}",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 3 }
            });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountA;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var result = await controller.GetIndicators(plan1, dbContext, currentUser, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IEnumerable<MonitoringController.IndicatorResponse>>(ok.Value);
        var responses = payload.ToList();
        Assert.Single(responses);
        Assert.Equal("Included", responses[0].Name);
    }

    [Fact]
    public async Task CreateIndicator_WithBlankName_ReturnsBadRequest()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var command = new CreateIndicatorCommand();

        var result = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(
                planId,
                null,
                "   ",
                null,
                null,
                null,
                null,
                "NotStarted",
                "{}"),
            command,
            dbContext,
            currentUser,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateIndicator_WithInvalidStatus_ReturnsBadRequest()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        var command = new CreateIndicatorCommand();

        var result = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(
                planId,
                null,
                "Indicator",
                null,
                null,
                null,
                null,
                "InvalidStatus",
                "{}"),
            command,
            dbContext,
            currentUser,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_blank_name()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("   ", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_invalid_status()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("N", null, null, null, null, "Bad", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_invalid_progress_percent_if_supported()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest(
                "N",
                null,
                null,
                null,
                null,
                "NotStarted",
                null,
                101m,
                null,
                null,
                RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Progress", bad.Value?.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_cross_tenant_indicator()
    {
        await using var db = CreateDbContext();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, owner, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = owner,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = other, UserId = Guid.NewGuid(), IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("X", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_deleted_indicator()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("X", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Archive_monitoring_indicator_sets_soft_delete_fields()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "Arch",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).ArchiveIndicator(id, archive, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        var reloaded = await db.MonitoringIndicators.SingleAsync(x => x.Id == id);
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.DeletedAtUtc);
        Assert.Equal(userId, reloaded.DeletedByUserId);
    }

    [Fact]
    public async Task Archive_monitoring_indicator_hides_indicator_from_plan_indicators_list()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "Gone",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await CreateController(out var _).ArchiveIndicator(id, archive, CancellationToken.None);

        var list = await CreateController(out var _).GetIndicators(planId, db, ctx, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(list);
        var payload = Assert.IsAssignableFrom<IEnumerable<MonitoringController.IndicatorResponse>>(ok.Value);
        Assert.Empty(payload);
    }

    [Fact]
    public async Task Archive_monitoring_indicator_rejects_cross_tenant_indicator()
    {
        await using var db = CreateDbContext();
        var owner = Guid.NewGuid();
        var other = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, owner, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = owner,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var ctx = new TestCurrentUserContext { AccountId = other, UserId = userId, IsAuthenticated = true };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        var result = await CreateController(out var _).ArchiveIndicator(id, archive, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Archive_monitoring_indicator_does_not_delete_monitoring_updates()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var indicatorId = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = accountId,
            PlanId = planId,
            Name = "With update",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        var updateId = Guid.NewGuid();
        db.MonitoringUpdates.Add(new MonitoringUpdate
        {
            Id = updateId,
            AccountId = accountId,
            MonitoringIndicatorId = indicatorId,
            PeriodLabel = "Q1",
            Status = "NotStarted",
            ReportedAtUtc = DateTimeOffset.UtcNow,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 }
        });
        await db.SaveChangesAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await CreateController(out var _).ArchiveIndicator(indicatorId, archive, CancellationToken.None);

        Assert.Equal(1, await db.MonitoringUpdates.CountAsync(u => u.Id == updateId));
    }

    [Fact]
    public async Task Update_monitoring_indicator_writes_audit_log_with_old_and_new_values()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "Old",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        _ = await CreateController(out var _).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("New", null, null, null, null, "InProgress", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            ctx,
            CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(a => a.EntityId == id && a.Action == "MonitoringIndicatorUpdated");
        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("MonitoringIndicator", log.EntityName);
        Assert.Equal("Old", log.OldValuesJson!.RootElement.GetProperty("name").GetString());
        Assert.Equal("New", log.NewValuesJson!.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Archive_monitoring_indicator_writes_audit_log()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await SeedPlanAsync(db, accountId, planId);
        var id = Guid.NewGuid();
        db.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = id,
            AccountId = accountId,
            PlanId = planId,
            Name = "N",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await CreateController(out var _).ArchiveIndicator(id, archive, CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(a => a.EntityId == id && a.Action == "MonitoringIndicatorArchived");
        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("MonitoringIndicator", log.EntityName);
        Assert.True(log.NewValuesJson!.RootElement.GetProperty("isDeleted").GetBoolean());
    }

    private static MonitoringController CreateController(out TestCurrentUserContext currentUser)
    {
        currentUser = new TestCurrentUserContext();
        return new MonitoringController();
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"monitoring-tests-{Guid.NewGuid():N}")
            .Options;
        return new MonitoringControllerTestDbContext(options);
    }

    private static async Task SeedPlanAsync(LccapDbContext dbContext, Guid accountId, Guid planId)
    {
        dbContext.Plans.Add(new Plan
        {
            Id = planId,
            AccountId = accountId,
            Title = "Test Plan",
            StartYear = 2025,
            EndYear = 2030,
            Status = "Draft",
            TemplateMode = "New",
            VersionNumber = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }
        });
        await dbContext.SaveChangesAsync();
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId { get; set; }

        public Guid? AccountId { get; set; }

        public bool IsAuthenticated { get; set; }
    }

    private sealed class MonitoringControllerTestDbContext : LccapDbContext
    {
        public MonitoringControllerTestDbContext(DbContextOptions<LccapDbContext> options)
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
