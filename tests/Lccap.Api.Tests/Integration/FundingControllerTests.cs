using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Funding.Commands;
using Lccap.Application.Funding.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class FundingControllerTests
{
    [Fact]
    public async Task GetClimateExpenditureTags_returns_only_current_account_tags()
    {
        using var db = CreateDbContext();
        var myAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(otherAccount, "OTHER-A", "Other A", "Other", active: true, deleted: false));
        var mine = NewTag(myAccount, "MY-A", "Mine A", "Adaptation", active: true, deleted: false);
        _ = db.ClimateExpenditureTags.Add(mine);
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(myAccount, Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal(mine.Id, payload.Items[0].Id);
        Assert.Equal("MY-A", payload.Items[0].TagCode);
    }

    [Fact]
    public async Task GetClimateExpenditureTags_excludes_soft_deleted_tags()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "X", "Alive", "Other", active: true, deleted: false));
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "Y", "Gone", "Other", active: false, deleted: true));
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, Guid.NewGuid(), true, WorkspaceRoles.Planner);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);

        foreach (var includeInactive in new[] { false, true })
        {
            var result = await new FundingController(ctx).GetClimateExpenditureTags(query, includeInactive, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
            Assert.Single(payload.Items);
            Assert.Equal("X", payload.Items[0].TagCode);
        }
    }

    [Fact]
    public async Task GetClimateExpenditureTags_excludes_inactive_when_default()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "ACT", "Active", "Other", active: true, deleted: false));
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "OFF", "Off", "Other", active: false, deleted: false));
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, Guid.NewGuid(), true, WorkspaceRoles.Admin);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
        Assert.Single(payload.Items);
        Assert.Equal("ACT", payload.Items[0].TagCode);
        Assert.False(payload.IncludeInactive);
    }

    [Fact]
    public async Task GetClimateExpenditureTags_includeInactive_includes_inactive_tags()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "ACT", "Active", "Other", active: true, deleted: false));
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "OFF", "Off", "Other", active: false, deleted: false));
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, Guid.NewGuid(), true, WorkspaceRoles.Reviewer);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, includeInactive: true, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
        Assert.Equal(2, payload.TotalCount);
        Assert.True(payload.IncludeInactive);
        Assert.Contains(payload.Items, i => i.TagCode == "ACT");
        Assert.Contains(payload.Items, i => i.TagCode == "OFF");
    }

    [Fact]
    public async Task GetClimateExpenditureTags_orders_by_category_code_name()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, code: "M2", name: "N", category: "Mitigation", active: true, deleted: false));
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, code: "A1", name: "Z", category: "Adaptation", active: true, deleted: false));
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, code: "A0", name: "A", category: "Adaptation", active: true, deleted: false));
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
        Assert.Equal(new[] { "A0", "A1", "M2" }, payload.Items.Select(i => i.TagCode).ToArray());
    }

    [Fact]
    public async Task Non_read_role_returns_forbid_before_query()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(accountId, "X", "X", "Other", active: true, deleted: false));
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, Guid.NewGuid(), true, role: WorkspaceRoles.PublicViewer);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, false, CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Missing_account_id_returns_empty_result()
    {
        using var db = CreateDbContext();
        var strayAccount = Guid.NewGuid();
        _ = db.ClimateExpenditureTags.Add(NewTag(strayAccount, "X", "X", "Other", active: true, deleted: false));
        _ = await db.SaveChangesAsync();

        var ctx = new TestMissingAccountCurrentUser(Guid.NewGuid(), true, WorkspaceRoles.Admin);
        var query = new GetClimateExpenditureTagsQuery(db, ctx);
        var result = await new FundingController(ctx).GetClimateExpenditureTags(query, false, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetClimateExpenditureTagsResult>(ok.Value);
        Assert.Empty(payload.Items);
        Assert.Equal(0, payload.TotalCount);
    }

    [Fact]
    public async Task PostFundingAllocation_creates_allocation_for_same_tenant_plan_action_source()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(
            actionId,
            sourceId,
            FundingProgramId: null,
            ClimateExpenditureTagId: null,
            FiscalYear: 2026,
            AllocatedAmount: 100m,
            CurrencyCode: null,
            AllocationStatus: null,
            Notes: null);

        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planId, body, cmd, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, ok.StatusCode);
        var dto = Assert.IsType<ActionFundingAllocationListItemDto>(ok.Value);
        Assert.Equal(planId, dto.PlanId);
        Assert.Equal(actionId, dto.ActionItemId);
        Assert.Equal("PHP", dto.CurrencyCode);
        Assert.Equal("Planned", dto.AllocationStatus);

        Assert.Equal(1, await db.ActionFundingAllocations.CountAsync(a => !a.IsDeleted && a.AccountId == accountId));
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_action_item_from_another_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planA = NewPlan(accountId, "Plan A");
        var planB = NewPlan(accountId, "Plan B");
        _ = db.Plans.Add(planA);
        _ = db.Plans.Add(planB);
        var actionOnB = NewAction(accountId, planB.Id);
        var source = NewSource(accountId);
        _ = db.ActionItems.Add(actionOnB);
        _ = db.FundingSources.Add(source);
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(actionOnB.Id, source.Id, null, null, 2026, 10m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planA.Id, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Action item does not belong to this plan.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_funding_source_from_other_tenant()
    {
        using var db = CreateDbContext();
        var myAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = NewPlan(myAccount);
        _ = db.Plans.Add(plan);
        var action = NewAction(myAccount, plan.Id);
        _ = db.ActionItems.Add(action);
        var otherSource = NewSource(otherAccount, "Other bank");
        _ = db.FundingSources.Add(otherSource);
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(myAccount, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(action.Id, otherSource.Id, null, null, 2026, 10m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(plan.Id, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Funding source was not found.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_program_linked_to_different_source()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = NewPlan(accountId);
        _ = db.Plans.Add(plan);
        var action = NewAction(accountId, plan.Id);
        var sourceA = NewSource(accountId, "A");
        var sourceB = NewSource(accountId, "B");
        var progOnA = NewProgram(accountId, sourceA.Id);

        _ = db.ActionItems.Add(action);
        _ = db.FundingSources.Add(sourceA);
        _ = db.FundingSources.Add(sourceB);
        _ = db.FundingPrograms.Add(progOnA);
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(action.Id, sourceB.Id, progOnA.Id, null, 2026, 5m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(plan.Id, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Funding program does not belong to the selected funding source.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_inactive_ccet_tag()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var inactiveTag = NewTag(accountId, "OFF", "Off", "Other", active: false, deleted: false);
        _ = db.ClimateExpenditureTags.Add(inactiveTag);
        _ = await db.SaveChangesAsync();

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(actionId, sourceId, null, inactiveTag.Id, 2026, 1m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planId, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Climate expenditure tag must be active.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_fiscal_year_out_of_range()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(actionId, sourceId, null, null, FiscalYear: 1999, 1m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planId, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Fiscal year must be between 2000 and 2100 inclusive.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_negative_allocated_amount()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(actionId, sourceId, null, null, 2026, -0.01m, null, null, null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planId, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Allocated amount cannot be negative.");
    }

    [Fact]
    public async Task PostFundingAllocation_rejects_non_Planned_status_on_create()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new CreateActionFundingAllocationCommand(db, ctx);
        var body = new CreateActionFundingAllocationApiRequest(actionId, sourceId, null, null, 2026, 1m, null, "Committed", null);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.CreateAllocation(planId, body, cmd, CancellationToken.None);

        AssertBadRequestContaining(result, "Only Planned status is allowed when creating allocations in this slice.");
    }

    [Fact]
    public async Task GetAllocationsByPlan_returns_only_same_plan_and_tenant()
    {
        using var db = CreateDbContext();
        var accountA = Guid.NewGuid();
        var accountB = Guid.NewGuid();
        var userA = Guid.NewGuid();

        var planA1 = NewPlan(accountA, "P1");
        var planA2 = NewPlan(accountA, "P2");
        var planB = NewPlan(accountB, "Other");
        db.Plans.AddRange(planA1, planA2, planB);

        var a1 = NewAction(accountId: accountA, planA1.Id, "A1");
        var a2 = NewAction(accountId: accountA, planA2.Id, "A2");
        var b1 = NewAction(accountId: accountB, planB.Id, "B1");
        db.ActionItems.AddRange(a1, a2, b1);

        var sA = NewSource(accountA);
        var sB = NewSource(accountB);
        db.FundingSources.AddRange(sA, sB);
        _ = await db.SaveChangesAsync();

        _ = await AddAllocationAsync(db, accountA, planA1.Id, a1.Id, sA.Id, fiscalYear: 2025);
        _ = await AddAllocationAsync(db, accountA, planA2.Id, a2.Id, sA.Id, fiscalYear: 2025);
        _ = await AddAllocationAsync(db, accountB, planB.Id, b1.Id, sB.Id, fiscalYear: 2025);

        var ctx = new TestCurrentUserContext(accountA, userA, true, WorkspaceRoles.Viewer);
        var q = new GetActionFundingAllocationsByPlanQuery(db, ctx);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.GetAllocationsByPlan(planA1.Id, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetActionFundingAllocationsListResult>(ok.Value!);
        Assert.Single(payload.Items);
        Assert.Equal(planA1.Id, payload.Items[0].PlanId);
    }

    [Fact]
    public async Task GetAllocationsByAction_returns_only_allocations_for_that_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = NewPlan(accountId);
        _ = db.Plans.Add(plan);
        var act1 = NewAction(accountId, plan.Id, "One");
        var act2 = NewAction(accountId, plan.Id, "Two");
        db.ActionItems.AddRange(act1, act2);
        var src = NewSource(accountId);
        _ = db.FundingSources.Add(src);
        _ = await db.SaveChangesAsync();

        _ = await AddAllocationAsync(db, accountId, plan.Id, act1.Id, src.Id, fiscalYear: 2026, amount: 10m);
        _ = await AddAllocationAsync(db, accountId, plan.Id, act2.Id, src.Id, fiscalYear: 2026, amount: 20m);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer);
        var q = new GetActionFundingAllocationsByActionQuery(db, ctx);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.GetAllocationsByActionItem(act2.Id, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetActionFundingAllocationsListResult>(ok.Value!);
        Assert.Single(payload.Items);
        Assert.Equal(act2.Id, payload.Items[0].ActionItemId);
    }

    [Fact]
    public async Task GetAllocations_exclude_soft_deleted()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);

        var alloc1 = await AddAllocationAsync(db, accountId, planId, actionId, sourceId, fiscalYear: 2025, allocationId: Guid.NewGuid());
        _ = await AddAllocationAsync(db, accountId, planId, actionId, sourceId, fiscalYear: 2026, allocationId: Guid.NewGuid());

        var archCtx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var archCmd = new ArchiveActionFundingAllocationCommand(db, archCtx);
        await archCmd.ExecuteAsync(alloc1.Id, CancellationToken.None);

        var readCtx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer);
        var q = new GetActionFundingAllocationsByPlanQuery(db, readCtx);
        var ctrl = new FundingController(readCtx);
        var result = await ctrl.GetAllocationsByPlan(planId, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<GetActionFundingAllocationsListResult>(ok.Value!);
        Assert.Single(payload.Items);
        Assert.Equal(2026, payload.Items[0].FiscalYear);
    }

    [Fact]
    public async Task DeleteFundingAllocation_soft_deletes_row()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountId);
        var alloc = await AddAllocationAsync(db, accountId, planId, actionId, sourceId, 2026);

        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);
        var cmd = new ArchiveActionFundingAllocationCommand(db, ctx);
        var ctrl = new FundingController(ctx);
        var del = await ctrl.ArchiveAllocation(alloc.Id, cmd, CancellationToken.None);

        Assert.IsType<NoContentResult>(del);
        var row = await db.ActionFundingAllocations.AsNoTracking().SingleAsync(a => a.Id == alloc.Id);
        Assert.True(row.IsDeleted);
        Assert.NotNull(row.DeletedAtUtc);
        Assert.Equal(userId, row.DeletedByUserId);
    }

    [Fact]
    public async Task DeleteFundingAllocation_other_tenant_returns_not_found()
    {
        using var db = CreateDbContext();
        var accountMine = Guid.NewGuid();
        var accountOther = Guid.NewGuid();

        var (planId, actionId, sourceId) = await SeedPlanActionSourceAsync(db, accountOther);
        var alloc = await AddAllocationAsync(db, accountOther, planId, actionId, sourceId, 2026);

        var ctx = new TestCurrentUserContext(accountMine, Guid.NewGuid(), true, WorkspaceRoles.Planner);
        var cmd = new ArchiveActionFundingAllocationCommand(db, ctx);
        var ctrl = new FundingController(ctx);
        var del = await ctrl.ArchiveAllocation(alloc.Id, cmd, CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(del);
    }

    [Fact]
    public async Task GetAllocationsByPlan_returns_not_found_for_unknown_plan()
    {
        using var db = CreateDbContext();
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var q = new GetActionFundingAllocationsByPlanQuery(db, ctx);
        var ctrl = new FundingController(ctx);
        var result = await ctrl.GetAllocationsByPlan(Guid.NewGuid(), q, CancellationToken.None);
        _ = Assert.IsType<NotFoundResult>(result);
    }

    private sealed class TestMissingAccountCurrentUser : ICurrentUserContext
    {
        public TestMissingAccountCurrentUser(Guid userId, bool isAuthenticated, string? role)
        {
            UserId = userId;
            IsAuthenticated = isAuthenticated;
            Role = role;
        }

        public Guid? AccountId => null;

        public Guid? UserId { get; }

        public string? Role { get; }

        public bool IsAuthenticated { get; }
    }

    private static void AssertBadRequestContaining(IActionResult result, string fragment)
    {
        var br = Assert.IsType<BadRequestObjectResult>(result);
        var prop = br.Value!.GetType().GetProperty("errors");
        Assert.NotNull(prop);
        var raw = prop.GetValue(br.Value);
        var errors = Assert.IsAssignableFrom<IEnumerable<string>>(raw);
        Assert.Contains(errors, e => e.Contains(fragment, StringComparison.Ordinal));
    }

    private static async Task<(Guid PlanId, Guid ActionItemId, Guid FundingSourceId)> SeedPlanActionSourceAsync(
        LccapDbContext db,
        Guid accountId)
    {
        var plan = NewPlan(accountId);
        _ = db.Plans.Add(plan);
        var action = NewAction(accountId, plan.Id);
        _ = db.ActionItems.Add(action);
        var source = NewSource(accountId);
        _ = db.FundingSources.Add(source);
        _ = await db.SaveChangesAsync();
        return (plan.Id, action.Id, source.Id);
    }

    private static async Task<ActionFundingAllocation> AddAllocationAsync(
        LccapDbContext db,
        Guid accountId,
        Guid planId,
        Guid actionItemId,
        Guid fundingSourceId,
        int fiscalYear,
        decimal amount = 50m,
        Guid? allocationId = null)
    {
        var entity = new ActionFundingAllocation
        {
            Id = allocationId ?? Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = actionItemId,
            FundingSourceId = fundingSourceId,
            FundingProgramId = null,
            FundingApplicationId = null,
            ClimateExpenditureTagId = null,
            FiscalYear = fiscalYear,
            AllocatedAmount = amount,
            CommittedAmount = null,
            ReleasedAmount = null,
            SpentAmount = null,
            CurrencyCode = "PHP",
            AllocationStatus = "Planned",
            Notes = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
        };
        entity.EnsureRowVersion();
        _ = db.ActionFundingAllocations.Add(entity);
        _ = await db.SaveChangesAsync();
        return entity;
    }

    private static Plan NewPlan(Guid accountId, string title = "Seed plan", Guid? id = null)
    {
        var plan = new Plan
        {
            Id = id ?? Guid.NewGuid(),
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

        return plan;
    }

    private static ActionItem NewAction(Guid accountId, Guid planId, string title = "Action")
    {
        var action = new ActionItem
        {
            AccountId = accountId,
            PlanId = planId,
            Title = title,
            Description = null,
            ActionType = "Adaptation",
            Sector = "General",
            ResponsibleOffice = null,
            BudgetAmount = 0,
            FundingSource = null,
            TimelineStartUtc = null,
            TimelineEndUtc = null,
            Kpi = null,
            PriorityScore = null,
            Status = "Planned",
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        action.EnsureRowVersion();
        return action;
    }

    private static FundingSource NewSource(Guid accountId, string name = "LGUs")
    {
        var s = new FundingSource
        {
            AccountId = accountId,
            Name = name,
            SourceType = "LGUInternal",
            Description = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        s.EnsureRowVersion();
        return s;
    }

    private static FundingProgram NewProgram(Guid accountId, Guid fundingSourceId, string name = "Program")
    {
        var p = new FundingProgram
        {
            AccountId = accountId,
            FundingSourceId = fundingSourceId,
            Name = name,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Status = "Active",
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        p.EnsureRowVersion();
        return p;
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"funding-tests-{Guid.NewGuid():N}")
            .Options;

        return new FundingTestDbContext(options);
    }

    private static ClimateExpenditureTag NewTag(
        Guid accountId,
        string code,
        string name,
        string category,
        bool active,
        bool deleted)
    {
        var tag = new ClimateExpenditureTag
        {
            AccountId = accountId,
            TagCode = code,
            TagName = name,
            TagCategory = category,
            WeightPercent = null,
            Description = null,
            IsActive = active,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = deleted,
            DeletedAtUtc = deleted ? DateTimeOffset.UtcNow : null,
            DeletedByUserId = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            UpdatedAtUtc = null,
        };
        tag.EnsureRowVersion();
        return tag;
    }

    private sealed class FundingTestDbContext : LccapDbContext
    {
        public FundingTestDbContext(DbContextOptions<LccapDbContext> options)
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
