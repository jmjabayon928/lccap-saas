using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring.Commands;
using Lccap.Application.Monitoring.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class MonitoringControllerTests
{
    private static decimal? ReadDecimalOrNull(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(propertyName, out var p))
        {
            return null;
        }
        return p.ValueKind switch
        {
            JsonValueKind.Number => p.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(p.GetString(), out var d) ? d : null,
            _ => null,
        };
    }

    private static string? ReadStringOrNull(string json, string propertyName)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty(propertyName, out var p) || p.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        return p.GetString();
    }

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
        currentUser.Role = WorkspaceRoles.Admin;
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
        currentUser.Role = WorkspaceRoles.Admin;
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
        currentUser.Role = WorkspaceRoles.Admin;
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
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<MonitoringController.IndicatorResponse>(ok.Value);
        Assert.Equal("After", body.Name);
        Assert.Equal("OnTrack", body.Status);
        var updated = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal("After", updated.Name);
    }

    [Fact]
    public async Task Create_monitoring_update_creates_row_and_updates_indicator_status()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q1 2026",
                ActualValue: 12.5m,
                ProgressPercent: 40m,
                Status: "OnTrack",
                Notes: "Quarterly update"),
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<MonitoringController.MonitoringUpdateResponse>(ok.Value);
        Assert.Equal(indicatorId, body.MonitoringIndicatorId);
        Assert.Equal("Q1 2026", body.PeriodLabel);
        Assert.Equal("OnTrack", body.Status);
        Assert.False(string.IsNullOrWhiteSpace(body.RowVersion));

        Assert.Equal(1, await dbContext.MonitoringUpdates.CountAsync());
        var updatedIndicator = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal("OnTrack", updatedIndicator.Status);
    }

    [Fact]
    public async Task Create_monitoring_update_publishes_workspace_notification_to_reviewer_not_actor()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();
        var plannerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        SeedNotifyAccountAndUsers(dbContext, accountId, plannerId, reviewerId);
        await SeedPlanAsync(dbContext, accountId, planId);
        dbContext.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = accountId,
            PlanId = planId,
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = plannerId;
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Planner;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q1 2026",
                ActualValue: 12.5m,
                ProgressPercent: 40m,
                Status: "OnTrack",
                Notes: "Quarterly update"),
            command,
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);

        var ev = await dbContext.NotificationEvents.SingleAsync(e => e.EventType == "MonitoringUpdateCreated");
        var updateId = (await dbContext.MonitoringUpdates.SingleAsync()).Id;
        Assert.Equal(updateId.ToString("D"), ev.PayloadJson.RootElement.GetProperty("entityId").GetString());

        var uns = await dbContext.UserNotifications.Where(n => n.NotificationEventId == ev.Id).ToListAsync();
        Assert.Single(uns);
        Assert.Equal(reviewerId, uns[0].UserId);
    }

    [Fact]
    public async Task Create_monitoring_update_cannot_cross_tenant_returns_not_found()
    {
        var ownerAccountId = Guid.NewGuid();
        var callerAccountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, ownerAccountId, planId);
        dbContext.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = ownerAccountId,
            PlanId = planId,
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = callerAccountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q1 2026",
                ActualValue: null,
                ProgressPercent: null,
                Status: "InProgress",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(0, await dbContext.MonitoringUpdates.CountAsync());
    }

    [Fact]
    public async Task Create_monitoring_update_does_not_wipe_indicator_frequency_or_responsible_office()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{\"frequency\":\"Monthly\",\"responsibleOffice\":\"ENRO\",\"currentValue\":10,\"progressPercent\":20}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q2 2026",
                ActualValue: 12m,
                ProgressPercent: 30m,
                Status: "InProgress",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);

        var updated = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal("Monthly", ReadStringOrNull(updated.MetadataJson, "frequency"));
        Assert.Equal("ENRO", ReadStringOrNull(updated.MetadataJson, "responsibleOffice"));
    }

    [Fact]
    public async Task Create_monitoring_update_with_null_actual_and_progress_preserves_indicator_snapshot_values()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{\"frequency\":\"Monthly\",\"responsibleOffice\":\"ENRO\",\"currentValue\":77.5,\"progressPercent\":55}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q3 2026",
                ActualValue: null,
                ProgressPercent: null,
                Status: "OnTrack",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);

        var updated = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal(77.5m, ReadDecimalOrNull(updated.MetadataJson, "currentValue"));
        Assert.Equal(55m, ReadDecimalOrNull(updated.MetadataJson, "progressPercent"));
        Assert.Equal("Monthly", ReadStringOrNull(updated.MetadataJson, "frequency"));
        Assert.Equal("ENRO", ReadStringOrNull(updated.MetadataJson, "responsibleOffice"));
    }

    [Fact]
    public async Task Create_monitoring_update_rejects_blank_period_label()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "   ",
                ActualValue: null,
                ProgressPercent: null,
                Status: "NotStarted",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_monitoring_update_rejects_invalid_status()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q1",
                ActualValue: null,
                ProgressPercent: null,
                Status: "Bad",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Create_monitoring_update_rejects_progress_percent_over_100()
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
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var command = new CreateMonitoringUpdateCommand(dbContext, currentUser);
        var result = await controller.CreateUpdate(
            indicatorId,
            new MonitoringController.CreateMonitoringUpdateApiRequest(
                PeriodLabel: "Q1",
                ActualValue: null,
                ProgressPercent: 101m,
                Status: "NotStarted",
                Notes: null),
            command,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_updates_returns_newest_first_excludes_soft_deleted_and_scopes_to_account()
    {
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var indicatorId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountA, planId);
        dbContext.MonitoringIndicators.Add(new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = accountA,
            PlanId = planId,
            Name = "Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 7, 7, 7, 7, 7, 7, 7, 7 }
        });

        var t1 = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero);

        dbContext.MonitoringUpdates.AddRange(
            new MonitoringUpdate
            {
                Id = Guid.NewGuid(),
                AccountId = accountA,
                MonitoringIndicatorId = indicatorId,
                PeriodLabel = "Old",
                Status = "NotStarted",
                ReportedAtUtc = t1,
                CreatedAtUtc = t1,
                IsDeleted = false,
                RowVersion = new byte[] { 1 }
            },
            new MonitoringUpdate
            {
                Id = Guid.NewGuid(),
                AccountId = accountA,
                MonitoringIndicatorId = indicatorId,
                PeriodLabel = "New",
                Status = "OnTrack",
                ReportedAtUtc = t2,
                CreatedAtUtc = t2,
                IsDeleted = false,
                RowVersion = new byte[] { 2 }
            },
            new MonitoringUpdate
            {
                Id = Guid.NewGuid(),
                AccountId = accountA,
                MonitoringIndicatorId = indicatorId,
                PeriodLabel = "Deleted",
                Status = "InProgress",
                ReportedAtUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                CreatedAtUtc = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = true,
                RowVersion = new byte[] { 3 }
            },
            new MonitoringUpdate
            {
                Id = Guid.NewGuid(),
                AccountId = accountB,
                MonitoringIndicatorId = indicatorId,
                PeriodLabel = "OtherTenant",
                Status = "Completed",
                ReportedAtUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                CreatedAtUtc = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero),
                IsDeleted = false,
                RowVersion = new byte[] { 4 }
            });

        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountA;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var query = new GetMonitoringUpdatesByIndicatorQuery(dbContext, currentUser);
        var result = await controller.GetUpdates(indicatorId, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<GetMonitoringUpdatesByIndicatorQuery.MonitoringUpdateListItem>>(ok.Value);
        var items = list.ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("New", items[0].PeriodLabel);
        Assert.Equal("Old", items[1].PeriodLabel);
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
        currentUser.Role = WorkspaceRoles.Admin;
        var indicatorsQuery = new GetIndicatorsByPlanQuery(dbContext, currentUser);
        var result = await controller.GetIndicators(plan1, null, null, indicatorsQuery, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var itemsProp = value.GetType().GetProperty("items");
        var payload = itemsProp != null
            ? (IEnumerable<MonitoringController.IndicatorResponse>)itemsProp.GetValue(value)!
            : Assert.IsAssignableFrom<IEnumerable<MonitoringController.IndicatorResponse>>(value);
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
        currentUser.Role = WorkspaceRoles.Admin;
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
        currentUser.Role = WorkspaceRoles.Admin;
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("   ", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("N", null, null, null, null, "Bad", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).UpdateIndicator(
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
        var ctx = new TestCurrentUserContext { AccountId = other, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("X", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_deleted_indicator()
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
            IsDeleted = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();
        var row = await db.MonitoringIndicators.SingleAsync();
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("X", null, null, null, null, "NotStarted", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).ArchiveIndicator(id, archive, CancellationToken.None);

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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await new MonitoringController(ctx).ArchiveIndicator(id, archive, CancellationToken.None);

        var indicatorsQuery = new GetIndicatorsByPlanQuery(db, ctx);
        var list = await new MonitoringController(ctx).GetIndicators(planId, null, null, indicatorsQuery, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(list);
        var value = ok.Value!;
        var itemsProp = value.GetType().GetProperty("items");
        var payload = itemsProp != null
            ? (IEnumerable<MonitoringController.IndicatorResponse>)itemsProp.GetValue(value)!
            : Assert.IsAssignableFrom<IEnumerable<MonitoringController.IndicatorResponse>>(value);
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
        var ctx = new TestCurrentUserContext { AccountId = other, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        var result = await new MonitoringController(ctx).ArchiveIndicator(id, archive, CancellationToken.None);

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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await new MonitoringController(ctx).ArchiveIndicator(indicatorId, archive, CancellationToken.None);

        Assert.Equal(1, await db.MonitoringUpdates.CountAsync(u => u.Id == updateId));
    }

    [Fact]
    public async Task Create_monitoring_indicator_returns_non_empty_row_version()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;
        var command = new CreateIndicatorCommand();

        var result = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(planId, null, "Indicator", null, null, null, null, "NotStarted", "{}"),
            command,
            dbContext,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<MonitoringController.IndicatorResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.RowVersion));
        var bytes = Convert.FromBase64String(body.RowVersion);
        Assert.Equal(8, bytes.Length);
        Assert.False(bytes.All(b => b == 0));
    }

    [Fact]
    public async Task Get_monitoring_indicators_repairs_legacy_empty_row_version_and_returns_non_empty_token()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var indicator = new MonitoringIndicator
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Name = "Legacy Indicator",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = Array.Empty<byte>() // Legacy empty token
        };
        dbContext.MonitoringIndicators.Add(indicator);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;

        var indicatorsQuery = new GetIndicatorsByPlanQuery(dbContext, currentUser);
        var result = await controller.GetIndicators(planId, null, null, indicatorsQuery, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var value = ok.Value!;
        var itemsProp = value.GetType().GetProperty("items");
        var list = (itemsProp != null
            ? (IEnumerable<MonitoringController.IndicatorResponse>)itemsProp.GetValue(value)!
            : Assert.IsAssignableFrom<IEnumerable<MonitoringController.IndicatorResponse>>(value)).ToList();
        var dto = list.Single(x => x.Id == indicator.Id);
        Assert.False(string.IsNullOrEmpty(dto.RowVersion));
        var bytes = Convert.FromBase64String(dto.RowVersion);
        Assert.Equal(8, bytes.Length);
        Assert.False(bytes.All(b => b == 0));

        // Verify it was saved to DB
        var reloaded = await dbContext.MonitoringIndicators.AsNoTracking().SingleAsync(i => i.Id == indicator.Id);
        Assert.Equal(bytes, reloaded.RowVersion);
    }

    [Fact]
    public async Task Update_monitoring_indicator_returns_non_empty_row_version()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var indicator = new MonitoringIndicator
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Name = "Before",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        var oldRowVersion = indicator.RowVersion.ToArray(); // Clone to avoid reference aliasing
        dbContext.MonitoringIndicators.Add(indicator);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;
        var updateCommand = new UpdateMonitoringIndicatorCommand(dbContext, currentUser);

        var result = await controller.UpdateIndicator(
            indicator.Id,
            new MonitoringController.UpdateIndicatorRequest("After", null, null, null, null, "InProgress", RowVersion: Convert.ToBase64String(indicator.RowVersion)),
            updateCommand,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<MonitoringController.IndicatorResponse>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.RowVersion));
        var bytes = Convert.FromBase64String(body.RowVersion);
        Assert.Equal(8, bytes.Length);
        Assert.NotEqual(oldRowVersion, bytes); // Should be rotated
    }

    [Fact]
    public async Task Update_monitoring_indicator_rejects_missing_row_version()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var indicator = new MonitoringIndicator
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Name = "Before",
            Status = "NotStarted",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        dbContext.MonitoringIndicators.Add(indicator);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(out var currentUser);
        currentUser.AccountId = accountId;
        currentUser.UserId = Guid.NewGuid();
        currentUser.IsAuthenticated = true;
        currentUser.Role = WorkspaceRoles.Admin;
        var updateCommand = new UpdateMonitoringIndicatorCommand(dbContext, currentUser);

        var result = await controller.UpdateIndicator(
            indicator.Id,
            new MonitoringController.UpdateIndicatorRequest("After", null, null, null, null, "InProgress", RowVersion: ""),
            updateCommand,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var cmd = new UpdateMonitoringIndicatorCommand(db, ctx);

        _ = await new MonitoringController(ctx).UpdateIndicator(
            id,
            new MonitoringController.UpdateIndicatorRequest("New", null, null, null, null, "InProgress", RowVersion: Convert.ToBase64String(row.RowVersion)),
            cmd,
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
        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = userId, IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var archive = new ArchiveMonitoringIndicatorCommand(db, ctx);

        _ = await new MonitoringController(ctx).ArchiveIndicator(id, archive, CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(a => a.EntityId == id && a.Action == "MonitoringIndicatorArchived");
        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("MonitoringIndicator", log.EntityName);
        Assert.True(log.NewValuesJson!.RootElement.GetProperty("isDeleted").GetBoolean());
    }

    [Fact]
    public async Task Viewer_cannot_create_update_or_archive_indicator()
    {
        var ctx = new TestCurrentUserContext { IsAuthenticated = true, Role = WorkspaceRoles.Viewer };
        var controller = new MonitoringController(ctx);

        var createResult = await controller.CreateIndicator(null!, null!, null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(createResult);

        var updateResult = await controller.UpdateIndicator(Guid.NewGuid(), null!, null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(updateResult);

        var archiveResult = await controller.ArchiveIndicator(Guid.NewGuid(), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(archiveResult);
    }

    [Fact]
    public async Task Planner_can_create_update_indicator()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Planner };
        var controller = new MonitoringController(ctx);

        var createResult = await controller.CreateIndicator(
            new MonitoringController.CreateIndicatorRequest(planId, null, "I", null, null, null, null, "NotStarted", "{}"),
            new CreateIndicatorCommand(),
            dbContext,
            CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(createResult);

        var indicatorId = ((MonitoringController.IndicatorResponse)((OkObjectResult)createResult).Value!).Id;
        var rowVersion = ((MonitoringController.IndicatorResponse)((OkObjectResult)createResult).Value!).RowVersion;

        var updateResult = await controller.UpdateIndicator(
            indicatorId,
            new MonitoringController.UpdateIndicatorRequest("I2", null, null, null, null, "OnTrack", RowVersion: rowVersion),
            new UpdateMonitoringIndicatorCommand(dbContext, ctx),
            CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(updateResult);
    }

    [Fact]
    public async Task Planner_cannot_archive_indicator()
    {
        var ctx = new TestCurrentUserContext { IsAuthenticated = true, Role = WorkspaceRoles.Planner };
        var controller = new MonitoringController(ctx);

        var result = await controller.ArchiveIndicator(Guid.NewGuid(), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Admin_can_archive_indicator()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);
        var indicatorId = Guid.NewGuid();
        dbContext.MonitoringIndicators.Add(new MonitoringIndicator { Id = indicatorId, AccountId = accountId, PlanId = planId, Name = "X", Status = "NotStarted", MetadataJson = "{}", RowVersion = new byte[8] });
        await dbContext.SaveChangesAsync();

        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        var controller = new MonitoringController(ctx);

        var result = await controller.ArchiveIndicator(indicatorId, new ArchiveMonitoringIndicatorCommand(dbContext, ctx), CancellationToken.None);
        _ = Assert.IsType<NoContentResult>(result);
    }

    private static MonitoringController CreateController(out TestCurrentUserContext currentUser)
    {
        currentUser = new TestCurrentUserContext { IsAuthenticated = true, Role = WorkspaceRoles.Admin };
        return new MonitoringController(currentUser);
    }

    private static void SeedNotifyAccountAndUsers(LccapDbContext db, Guid accountId, Guid plannerId, Guid reviewerId)
    {
        db.Accounts.Add(
            new Account
            {
                Id = accountId,
                Name = "T",
                Region = "R",
                Province = "P",
                MunicipalityOrCity = "M",
                LguType = "City",
                ContactEmail = "c@test",
                Status = "Active",
                SettingsJson = JsonDocument.Parse("{}"),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            });
        db.Users.AddRange(
            new User
            {
                Id = plannerId,
                AccountId = accountId,
                Email = "p@test",
                FullName = "P",
                PasswordHash = "-",
                Role = WorkspaceRoles.Planner,
                Status = "Active",
                UserScope = "Tenant",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 },
            },
            new User
            {
                Id = reviewerId,
                AccountId = accountId,
                Email = "r@test",
                FullName = "R",
                PasswordHash = "-",
                Role = WorkspaceRoles.Reviewer,
                Status = "Active",
                UserScope = "Tenant",
                CreatedAtUtc = DateTimeOffset.UtcNow,
                IsDeleted = false,
                RowVersion = new byte[] { 3, 2, 3, 4, 5, 6, 7, 8 },
            });
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

        public string? Role { get; set; }

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
