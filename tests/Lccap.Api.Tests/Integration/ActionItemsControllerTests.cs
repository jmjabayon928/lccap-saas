using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Actions.Commands;
using Lccap.Application.Actions.Queries;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class ActionItemsControllerTests
{
    [Fact]
    public async Task Create_action_succeeds_for_current_account_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(title: "Build resilience"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ActionItemDto>(created.Value);
        Assert.Equal(accountId, dto.AccountId);
        Assert.Equal(plan.Id, dto.PlanId);
        Assert.True(await db.ActionItems.AnyAsync(a => a.Id == dto.Id && a.AccountId == accountId));
    }

    [Fact]
    public async Task Blank_title_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(title: "   "),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Invalid_action_type_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(actionType: "Solar"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Negative_budget_amount_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(budgetAmount: -1m),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Invalid_timeline_range_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var start = new DateTimeOffset(2027, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var body = ValidCreateBody() with { TimelineStartUtc = start, TimelineEndUtc = end };

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            body,
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Cross_tenant_plan_create_returns_404()
    {
        using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, ownerAccount);
        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_actions_returns_only_current_account_and_plan_actions_sorted()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan1 = await SeedPlan(db, accountId, "Plan One");
        var plan2 = await SeedPlan(db, accountId, "Plan Two");
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var create = new CreateActionItemCommand(db, ctx);

        _ = await create.ExecuteAsync(
            plan1.Id,
            ToApp(ValidCreateBody(title: "Alpha", actionType: "Mitigation", sector: "Energy")),
            CancellationToken.None);
        _ = await create.ExecuteAsync(
            plan1.Id,
            ToApp(ValidCreateBody(title: "Zebra", actionType: "Adaptation", sector: "Water")),
            CancellationToken.None);
        _ = await create.ExecuteAsync(
            plan2.Id,
            ToApp(ValidCreateBody(title: "Other plan item", actionType: "Adaptation", sector: "Health")),
            CancellationToken.None);

        var result = await new ActionItemsController(ctx).ListForPlan(
            plan1.Id,
            new GetActionItemsByPlanQuery(db, ctx),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ActionItemDto>>(ok.Value);
        Assert.Equal(2, list.Count);
        Assert.All(list, x => Assert.Equal(accountId, x.AccountId));
        Assert.All(list, x => Assert.Equal(plan1.Id, x.PlanId));
        Assert.Equal("Alpha", list[0].Title);
        Assert.Equal("Zebra", list[1].Title);
    }

    [Fact]
    public async Task Get_action_by_id_returns_only_same_account_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(
            plan.Id,
            ToApp(ValidCreateBody()),
            CancellationToken.None);

        var result = await new ActionItemsController(ctx).GetById(
            created.Item!.Id,
            new GetActionItemByIdQuery(db, ctx),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ActionItemDto>(ok.Value);
        Assert.Equal(accountId, dto.AccountId);
        Assert.Equal(created.Item.Id, dto.Id);
    }

    [Fact]
    public async Task Cross_tenant_get_action_by_id_returns_404()
    {
        using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, ownerAccount);
        var ownerCtx = new TestCurrentUserContext(ownerAccount, userId, true, WorkspaceRoles.Admin);
        var created = await new CreateActionItemCommand(db, ownerCtx).ExecuteAsync(
            plan.Id,
            ToApp(ValidCreateBody()),
            CancellationToken.None);

        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).GetById(
            created.Item!.Id,
            new GetActionItemByIdQuery(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_action_succeeds_for_same_account_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(title: "Before"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id && a.Title == "Before");

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "After",
                "Desc",
                "Mitigation",
                "Forestry",
                null,
                100m,
                null,
                null,
                null,
                null,
                null,
                "InProgress",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);
        var updated = await db.ActionItems.SingleAsync(a => a.Id == persisted.Id);
        Assert.Equal("After", updated.Title);
        Assert.Equal("InProgress", updated.Status);
    }

    [Fact]
    public async Task Cross_tenant_update_returns_404()
    {
        using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, ownerAccount);
        var ownerCtx = new TestCurrentUserContext(ownerAccount, userId, true, WorkspaceRoles.Admin);
        await new ActionItemsController(ownerCtx).Create(
            plan.Id,
            ValidCreateBody(),
            new CreateActionItemCommand(db, ownerCtx),
            CancellationToken.None);

        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "X",
                null,
                "Adaptation",
                "Water",
                null,
                0m,
                null,
                null,
                null,
                null,
                null,
                "Planned",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_action_item_updates_allowed_fields_and_returns_updated_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(title: "Alpha"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);
        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "Beta",
                "D",
                "Mitigation",
                "Energy",
                "Office A",
                50m,
                "GAA",
                null,
                null,
                "KPI text",
                5m,
                "OnTrack",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ActionItemDto>(ok.Value);
        Assert.Equal("Beta", dto.Title);
        Assert.Equal("OnTrack", dto.Status);
        var reloaded = await db.ActionItems.SingleAsync(a => a.Id == persisted.Id);
        Assert.Equal("Mitigation", reloaded.ActionType);
        Assert.Equal(5m, reloaded.PriorityScore);
    }

    [Fact]
    public async Task Update_action_item_rejects_invalid_action_type()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "T",
                null,
                "Solar",
                "Water",
                null,
                0m,
                null,
                null,
                null,
                null,
                null,
                "Planned",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_action_item_rejects_invalid_status()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "T",
                null,
                "Adaptation",
                "Water",
                null,
                0m,
                null,
                null,
                null,
                null,
                null,
                "UnknownStatus",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_action_item_rejects_negative_budget()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "T",
                null,
                "Adaptation",
                "Water",
                null,
                -1m,
                null,
                null,
                null,
                null,
                null,
                "Planned",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_action_item_rejects_invalid_timeline()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var start = new DateTimeOffset(2028, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "T",
                null,
                "Adaptation",
                "Water",
                null,
                0m,
                null,
                start,
                end,
                null,
                null,
                "Planned",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_action_item_rejects_deleted_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);
        persisted.IsDeleted = true;
        _ = await db.SaveChangesAsync();

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "T",
                null,
                "Adaptation",
                "Water",
                null,
                0m,
                null,
                null,
                null,
                null,
                null,
                "Planned",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_action_returns_non_empty_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await new ActionItemsController(ctx).Create(
            plan.Id,
            ValidCreateBody(title: "New Action"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var dto = Assert.IsType<ActionItemDto>(created.Value);
        Assert.NotNull(dto.RowVersion);
        Assert.Equal(8, dto.RowVersion.Length);
        Assert.False(dto.RowVersion.All(b => b == 0));
    }

    [Fact]
    public async Task Get_action_or_list_repairs_legacy_empty_row_version_and_returns_non_empty_token()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var action = new ActionItem
        {
            AccountId = accountId,
            PlanId = plan.Id,
            Title = "Legacy Action",
            ActionType = "Adaptation",
            Sector = "Water",
            BudgetAmount = 0m,
            Status = "Planned",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = Array.Empty<byte>() // Legacy empty token
        };
        db.ActionItems.Add(action);
        await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var result = await new ActionItemsController(ctx).ListForPlan(
            plan.Id,
            new GetActionItemsByPlanQuery(db, ctx),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IReadOnlyList<ActionItemDto>>(ok.Value);
        var dto = list.Single(x => x.Id == action.Id);
        Assert.NotNull(dto.RowVersion);
        Assert.Equal(8, dto.RowVersion.Length);
        Assert.False(dto.RowVersion.All(b => b == 0));

        // Verify it was saved to DB
        var reloaded = await db.ActionItems.AsNoTracking().SingleAsync(a => a.Id == action.Id);
        Assert.Equal(dto.RowVersion, reloaded.RowVersion);
    }

    [Fact]
    public async Task Update_action_returns_non_empty_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(plan.Id, ToApp(ValidCreateBody()), CancellationToken.None);
        var persisted = created.Item!;

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "Updated",
                null,
                persisted.ActionType,
                persisted.Sector,
                null,
                persisted.BudgetAmount,
                null,
                null,
                null,
                null,
                null,
                persisted.Status,
                null,
                persisted.RowVersion),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ActionItemDto>(ok.Value);
        Assert.NotNull(dto.RowVersion);
        Assert.Equal(8, dto.RowVersion.Length);
        Assert.NotEqual(persisted.RowVersion, dto.RowVersion); // Should be rotated
    }

    [Fact]
    public async Task Update_action_rejects_missing_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(plan.Id, ToApp(ValidCreateBody()), CancellationToken.None);
        var persisted = created.Item!;

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "Updated",
                null,
                persisted.ActionType,
                persisted.Sector,
                null,
                persisted.BudgetAmount,
                null,
                null,
                null,
                null,
                null,
                persisted.Status,
                null,
                Array.Empty<byte>()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_action_with_stale_row_version_returns_conflict()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(plan.Id, ToApp(ValidCreateBody()), CancellationToken.None);
        var persisted = created.Item!;
        var staleVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };

        var result = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "Updated",
                null,
                persisted.ActionType,
                persisted.Sector,
                null,
                persisted.BudgetAmount,
                null,
                null,
                null,
                null,
                null,
                persisted.Status,
                null,
                staleVersion),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Archive_action_item_sets_soft_delete_fields()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(title: "To archive"), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var result = await new ActionItemsController(ctx).Archive(
            persisted.Id,
            new ArchiveActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NoContentResult>(result);
        var reloaded = await db.ActionItems.SingleAsync(a => a.Id == persisted.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.DeletedAtUtc);
        Assert.Equal(userId, reloaded.DeletedByUserId);
    }

    [Fact]
    public async Task Archive_action_item_hides_action_from_plan_actions_list()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(title: "Gone"), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        _ = await new ActionItemsController(ctx).Archive(
            persisted.Id,
            new ArchiveActionItemCommand(db, ctx),
            CancellationToken.None);

        var listOutcome = await new GetActionItemsByPlanQuery(db, ctx).ExecuteAsync(plan.Id, CancellationToken.None);
        Assert.True(listOutcome.IsSuccess);
        Assert.NotNull(listOutcome.Items);
        Assert.Empty(listOutcome.Items);
    }

    [Fact]
    public async Task Archive_action_item_rejects_cross_tenant_action()
    {
        using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, ownerAccount);
        var ownerCtx = new TestCurrentUserContext(ownerAccount, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ownerCtx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ownerCtx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);
        var result = await new ActionItemsController(ctx).Archive(
            persisted.Id,
            new ArchiveActionItemCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_action_item_writes_audit_log_with_old_and_new_values()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(title: "Old title"), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        _ = await new ActionItemsController(ctx).Update(
            persisted.Id,
            new UpdateActionItemApiRequest(
                "New title",
                null,
                "Adaptation",
                "Water",
                null,
                0m,
                null,
                null,
                null,
                null,
                null,
                "InProgress",
                null,
                persisted.RowVersion.ToArray()),
            new UpdateActionItemCommand(db, ctx),
            CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(
            a => a.EntityId == persisted.Id && a.Action == "ActionItemUpdated");
        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("ActionItem", log.EntityName);
        Assert.Equal("Old title", log.OldValuesJson!.RootElement.GetProperty("title").GetString());
        Assert.Equal("New title", log.NewValuesJson!.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Archive_action_item_writes_audit_log()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        _ = await new ActionItemsController(ctx).Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        _ = await new ActionItemsController(ctx).Archive(
            persisted.Id,
            new ArchiveActionItemCommand(db, ctx),
            CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(
            a => a.EntityId == persisted.Id && a.Action == "ActionItemArchived");
        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("ActionItem", log.EntityName);
        Assert.True(log.NewValuesJson!.RootElement.GetProperty("isDeleted").GetBoolean());
    }

    [Fact]
    public async Task Viewer_cannot_create_update_or_archive_action()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var controller = new ActionItemsController(ctx);

        var createResult = await controller.Create(Guid.NewGuid(), ValidCreateBody(), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(createResult);

        var updateResult = await controller.Update(Guid.NewGuid(), new UpdateActionItemApiRequest("T", null, "A", "S", null, 0, null, null, null, null, null, "P", null, new byte[8]), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(updateResult);

        var archiveResult = await controller.Archive(Guid.NewGuid(), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(archiveResult);
    }

    [Fact]
    public async Task Planner_can_create_update_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var controller = new ActionItemsController(ctx);

        var createResult = await controller.Create(plan.Id, ValidCreateBody(), new CreateActionItemCommand(db, ctx), CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var dto = Assert.IsType<ActionItemDto>(created.Value);

        var updateResult = await controller.Update(dto.Id, new UpdateActionItemApiRequest("T", null, "Adaptation", "Water", null, 0, null, null, null, null, null, "Planned", null, dto.RowVersion), new UpdateActionItemCommand(db, ctx), CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(updateResult);
    }

    [Fact]
    public async Task Planner_cannot_archive_action()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Planner);
        var controller = new ActionItemsController(ctx);

        var result = await controller.Archive(Guid.NewGuid(), null!, CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Admin_can_archive_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var controller = new ActionItemsController(ctx);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(plan.Id, ToApp(ValidCreateBody()), CancellationToken.None);

        var result = await controller.Archive(created.Item!.Id, new ArchiveActionItemCommand(db, ctx), CancellationToken.None);
        _ = Assert.IsType<NoContentResult>(result);
    }

    private static CreateActionItemApiRequest ValidCreateBody(
        string title = "Action title",
        string actionType = "Adaptation",
        string sector = "Water",
        decimal? budgetAmount = 0m) =>
        new(
            title,
            null,
            actionType,
            sector,
            null,
            budgetAmount,
            null,
            null,
            null,
            null,
            null,
            "Planned",
            null);

    private static CreateActionItemRequest ToApp(CreateActionItemApiRequest body)
    {
        JsonDocument? metadata = null;
        if (!string.IsNullOrWhiteSpace(body.MetadataJson))
        {
            metadata = JsonDocument.Parse(body.MetadataJson);
        }

        return new CreateActionItemRequest(
            body.Title,
            body.Description,
            body.ActionType,
            body.Sector,
            body.ResponsibleOffice,
            body.BudgetAmount,
            body.FundingSource,
            body.TimelineStartUtc,
            body.TimelineEndUtc,
            body.Kpi,
            body.PriorityScore,
            body.Status,
            metadata);
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"actions-tests-{Guid.NewGuid()}")
            .Options;

        return new ActionItemsTestDbContext(options);
    }

    private static async Task<Plan> SeedPlan(LccapDbContext db, Guid accountId, string title = "Test Plan")
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
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = db.Plans.Add(plan);
        _ = await db.SaveChangesAsync();
        return plan;
    }

    private sealed class ActionItemsTestDbContext : LccapDbContext
    {
        public ActionItemsTestDbContext(DbContextOptions<LccapDbContext> options)
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

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(Guid accountId, Guid userId, bool isAuthenticated, string? role = WorkspaceRoles.Admin)
        {
            AccountId = accountId;
            UserId = userId;
            IsAuthenticated = isAuthenticated;
            Role = role;
        }

        public Guid? AccountId { get; }

        public Guid? UserId { get; }

        public string? Role { get; }

        public bool IsAuthenticated { get; }
    }
}
