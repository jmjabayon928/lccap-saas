using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Maps.Commands;
using Lccap.Application.Maps.Queries;
using Lccap.Application.Plans.Commands;
using Lccap.Application.Plans.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using System.Text.Json;

namespace Lccap.Api.Tests.Integration;

public sealed class PlansControllerTests
{
    [Fact]
    public async Task Valid_create_succeeds_for_current_account()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("Test Plan", 2025, 2026, "Draft", "New", 1, "desc", null, null),
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(created.Value);
        Assert.Single(db.Plans.Where(p => p.AccountId == accountId));
    }

    [Fact]
    public async Task Create_plan_seeds_eight_default_sections_for_plan_and_account()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("Seeded Plan", 2025, 2026, "Draft", "New", 1, null, null, null),
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var planDto = Assert.IsType<PlanDto>(created.Value);
        var planId = planDto.Id;

        var sections = await db.PlanSections.Where(s => s.PlanId == planId && !s.IsDeleted).ToListAsync();
        Assert.Equal(8, sections.Count);

        foreach (var s in sections)
        {
            Assert.Equal(accountId, s.AccountId);
            Assert.Equal(planId, s.PlanId);
            Assert.False(s.IsDeleted);
            Assert.Equal(string.Empty, s.Content);
        }

        var orderedKeys = sections.OrderBy(s => s.SortOrder).Select(s => (s.SectionKey, s.SortOrder)).ToList();
        var expectedKeys = new (string Key, int Order)[]
        {
            ("executive_summary", 10),
            ("introduction", 20),
            ("climate_risk_assessment", 30),
            ("adaptation_actions", 40),
            ("mitigation_actions", 50),
            ("implementation_plan", 60),
            ("monitoring_evaluation", 70),
            ("references_annexes", 80),
        };
        Assert.Equal(expectedKeys.Select(x => x.Key), orderedKeys.Select(x => x.SectionKey));
        Assert.Equal(expectedKeys.Select(x => x.Order), orderedKeys.Select(x => x.SortOrder));

        Assert.False(
            await db.PlanSections.AnyAsync(s => s.PlanId == planId && s.AccountId == otherAccountId && !s.IsDeleted));
    }

    [Fact]
    public async Task Blank_title_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("   ", 2025, 2026, "Draft", "New", 1, null, null, null),
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Invalid_year_range_returns_400()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("Plan", 2101, 2102, "Draft", "New", 1, null, null, null),
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Get_plan_returns_only_same_account_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Same Account Plan");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlanById(
            seeded.Id,
            new GetPlanByIdQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task Cross_tenant_get_returns_404()
    {
        using var db = CreateDbContext();
        var ownerAccountId = Guid.NewGuid();
        var requesterAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, ownerAccountId, "Other Account Plan");
        var controller = CreateController(db, requesterAccountId, userId);

        var result = await controller.GetPlanById(
            seeded.Id,
            new GetPlanByIdQuery(db, new TestCurrentUserContext(requesterAccountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_plans_returns_only_current_account_non_deleted_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "One");
        _ = await SeedPlan(db, accountId, "Two");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Equal(2, plans.Count);
        Assert.Contains(plans, p => p.Title == "One");
        Assert.Contains(plans, p => p.Title == "Two");
    }

    [Fact]
    public async Task Get_plans_excludes_cross_tenant_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Mine");
        _ = await SeedPlan(db, otherAccountId, "Theirs");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Single(plans);
        Assert.Equal("Mine", plans[0].Title);
    }

    [Fact]
    public async Task Get_plans_excludes_deleted_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Active");
        _ = await SeedPlan(db, accountId, "Gone", isDeleted: true);
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Single(plans);
        Assert.Equal("Active", plans[0].Title);
    }

    [Fact]
    public async Task Get_plans_orders_newest_first()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var t0 = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        _ = await SeedPlan(db, accountId, "Oldest", createdAtUtc: t0, updatedAtUtc: null);
        _ = await SeedPlan(db, accountId, "Mid", createdAtUtc: t0, updatedAtUtc: t0.AddYears(3));
        _ = await SeedPlan(db, accountId, "Newest", createdAtUtc: t0.AddYears(10), updatedAtUtc: null);
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Equal(3, plans.Count);
        Assert.Equal("Newest", plans[0].Title);
        Assert.Equal("Mid", plans[1].Title);
        Assert.Equal("Oldest", plans[2].Title);
    }

    [Fact]
    public async Task Get_plans_returns_empty_list_when_no_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Empty(plans);
    }

    [Fact]
    public async Task Get_plans_does_not_accept_account_id_from_query_or_body()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Mine");
        _ = await SeedPlan(db, otherAccountId, "Theirs");
        var controller = CreateController(db, accountId, userId);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.QueryString = new QueryString("?accountId=" + Uri.EscapeDataString(otherAccountId.ToString()));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(result);
        Assert.Single(plans);
        Assert.Equal("Mine", plans[0].Title);
    }

    [Fact]
    public async Task Get_plans_returns_forbidden_when_account_context_missing()
    {
        using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, null, userId, WorkspaceRoles.Admin);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(null, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Update_succeeds_for_same_account_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var updated = await db.Plans.SingleAsync(p => p.Id == seeded.Id);
        Assert.Equal("After", updated.Title);
    }

    [Fact]
    public async Task Update_plan_metadata_rejects_invalid_year_range()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2027, 2025, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_plan_metadata_rejects_invalid_status()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InvalidStatus", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_plan_metadata_rejects_invalid_template_mode()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "InvalidMode", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_plan_metadata_rejects_non_positive_version_number()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 0, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_plan_metadata_writes_audit_log_with_old_and_new_values()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId);

        _ = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var auditLog = await db.AuditLogs.SingleOrDefaultAsync(l => l.EntityId == seeded.Id && l.Action == "PlanMetadataUpdated");
        Assert.NotNull(auditLog);
        Assert.Equal(accountId, auditLog.AccountId);
        Assert.Equal(userId, auditLog.UserId);
        Assert.Contains("Before", auditLog.OldValuesJson!.RootElement.GetRawText());
        Assert.Contains("After", auditLog.NewValuesJson!.RootElement.GetRawText());
    }

    [Fact]
    public async Task Archive_plan_sets_status_archived_and_soft_delete_fields()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId);

        var result = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NoContentResult>(result);
        var archived = await db.Plans.IgnoreQueryFilters().SingleAsync(p => p.Id == seeded.Id);
        Assert.Equal("Archived", archived.Status);
        Assert.True(archived.IsDeleted);
        Assert.NotNull(archived.DeletedAtUtc);
        Assert.Equal(userId, archived.DeletedByUserId);
    }

    [Fact]
    public async Task Archive_plan_hides_plan_from_get_plans_list()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId);

        _ = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var listResult = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var plans = AssertPlansList(listResult);
        Assert.Empty(plans);
    }

    [Fact]
    public async Task Archive_plan_makes_get_plan_by_id_return_not_found()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId);

        _ = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var getResult = await controller.GetPlanById(
            seeded.Id,
            new GetPlanByIdQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(getResult);
    }

    [Fact]
    public async Task Archive_plan_writes_audit_log()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId);

        _ = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var auditLog = await db.AuditLogs.SingleOrDefaultAsync(l => l.EntityId == seeded.Id && l.Action == "PlanArchived");
        Assert.NotNull(auditLog);
        Assert.Equal(accountId, auditLog.AccountId);
        Assert.Equal(userId, auditLog.UserId);
        Assert.Contains("Archived", auditLog.NewValuesJson!.RootElement.GetRawText());
    }

    [Fact]
    public async Task Archive_plan_rejects_cross_tenant_plan()
    {
        using var db = CreateDbContext();
        var ownerAccountId = Guid.NewGuid();
        var requesterAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, ownerAccountId, "Other Account Plan");
        var controller = CreateController(db, requesterAccountId, userId);

        var result = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, new TestCurrentUserContext(requesterAccountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_create_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Viewer);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("Test Plan", 2025, 2026, "Draft", "New", 1, "desc", null, null),
            new CreatePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_update_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Viewer);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_archive_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Viewer);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer);

        var result = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Planner_can_create_and_update_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Planner);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);

        var createResult = await controller.CreatePlan(
            new CreatePlanApiRequest("Planner Plan", 2025, 2026, "Draft", "New", 1, "desc", null, null),
            new CreatePlanCommand(db, ctx),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(createResult);
        var planDto = Assert.IsType<PlanDto>(created.Value);

        var updateResult = await controller.UpdatePlan(
            planDto.Id,
            new UpdatePlanApiRequest("Planner Updated", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, planDto.RowVersion),
            new UpdatePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(updateResult);
    }

    [Fact]
    public async Task Planner_cannot_archive_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Planner);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner);

        var result = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Admin_can_archive_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "To Archive");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        var result = await controller.ArchivePlan(
            seeded.Id,
            new ArchivePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Reviewer_can_read_but_cannot_update_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Reviewer);
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Reviewer);

        var getResult = await controller.GetPlanById(
            seeded.Id,
            new GetPlanByIdQuery(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(getResult);

        var updateResult = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, ctx),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(updateResult);
    }

    [Fact]
    public async Task Update_plan_metadata_rejects_deleted_plan()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Deleted Plan", isDeleted: true);
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_plan_returns_non_empty_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);

        var result = await controller.CreatePlan(
            new CreatePlanApiRequest("Test Plan", 2025, 2026, "Draft", "New", 1, "desc", null, null),
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var planDto = Assert.IsType<PlanDto>(created.Value);
        Assert.NotNull(planDto.RowVersion);
        Assert.Equal(8, planDto.RowVersion.Length);
        Assert.False(planDto.RowVersion.All(b => b == 0));
    }

    [Fact]
    public async Task Get_plan_by_id_repairs_legacy_empty_row_version_and_returns_non_empty_token()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Title = "Legacy Plan",
            StartYear = 2025,
            EndYear = 2026,
            Status = "Draft",
            TemplateMode = "New",
            VersionNumber = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = Array.Empty<byte>() // Legacy empty token
        };
        db.Plans.Add(plan);
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var result = await controller.GetPlanById(
            plan.Id,
            new GetPlanByIdQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var planDto = Assert.IsType<PlanDto>(ok.Value);
        Assert.NotNull(planDto.RowVersion);
        Assert.Equal(8, planDto.RowVersion.Length);
        Assert.False(planDto.RowVersion.All(b => b == 0));

        // Verify it was saved to DB
        var reloaded = await db.Plans.AsNoTracking().SingleAsync(p => p.Id == plan.Id);
        Assert.Equal(planDto.RowVersion, reloaded.RowVersion);
    }

    [Fact]
    public async Task Update_plan_returns_non_empty_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var oldRowVersion = seeded.RowVersion.ToArray(); // Clone to avoid reference aliasing
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var planDto = Assert.IsType<PlanDto>(ok.Value);
        Assert.NotNull(planDto.RowVersion);
        Assert.Equal(8, planDto.RowVersion.Length);
        Assert.NotEqual(oldRowVersion, planDto.RowVersion); // Should be rotated
    }

    [Fact]
    public async Task Update_plan_rejects_missing_row_version()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Before");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, Array.Empty<byte>()),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_plan_with_stale_row_version_returns_conflict()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, accountId, "Stale Test");
        var staleVersion = new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 };
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, staleVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Planner_can_read_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Plan 1");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Planner);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner)),
            CancellationToken.None);

        _ = AssertPlansList(result);
    }

    [Fact]
    public async Task Viewer_can_read_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Plan 1");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Viewer);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer)),
            CancellationToken.None);

        _ = AssertPlansList(result);
    }

    [Fact]
    public async Task Reviewer_can_read_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _ = await SeedPlan(db, accountId, "Plan 1");
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Reviewer);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Reviewer)),
            CancellationToken.None);

        _ = AssertPlansList(result);
    }

    [Fact]
    public async Task Operational_dashboard_returns_not_found_for_cross_tenant_plan()
    {
        using var db = CreateDbContext();
        var ownerAccountId = Guid.NewGuid();
        var requesterAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, ownerAccountId, "Other Account Plan");
        var controller = CreateController(db, requesterAccountId, userId);

        var result = await controller.GetOperationalDashboard(
            seeded.Id,
            recentActivityLimit: null,
            new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(requesterAccountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Operational_dashboard_counts_evidence_linkage_and_statuses()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Evidence Plan");
        var section = NewPlanSection(accountId, plan.Id);
        _ = db.PlanSections.Add(section);
        var linkAction = NewAction(plan.Id, accountId, "Planned", budgetAmount: 0m, fundingSource: null);
        db.ActionItems.Add(linkAction);
        var file = SeedMinimalFile(accountId);
        file.OwnerId = plan.Id;
        _ = db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var docs = new[]
        {
            NewDocument(accountId, plan.Id, file.Id, "Draft", null, null),
            NewDocument(accountId, plan.Id, file.Id, "Internal", section.Id, null),
            NewDocument(accountId, plan.Id, file.Id, "Official", section.Id, linkAction.Id),
            NewDocument(accountId, plan.Id, file.Id, "Public", null, linkAction.Id),
        };
        db.Documents.AddRange(docs);
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetOperationalDashboard(plan.Id, null, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dash = Assert.IsType<PlanOperationalDashboardDto>(ok.Value);
        Assert.Equal(4, dash.Evidence.TotalDocuments);
        Assert.Equal(1, dash.Evidence.DraftEvidenceCount);
        Assert.Equal(1, dash.Evidence.InternalEvidenceCount);
        Assert.Equal(1, dash.Evidence.OfficialEvidenceCount);
        Assert.Equal(1, dash.Evidence.PublicEvidenceCount);
        Assert.Equal(2, dash.Evidence.LinkedToSectionCount);
        Assert.Equal(2, dash.Evidence.LinkedToActionCount);
    }

    [Fact]
    public async Task Operational_dashboard_counts_actions_and_monitoring_correctly()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Counts Plan");

        db.ActionItems.AddRange(
            NewAction(plan.Id, accountId, "Planned", budgetAmount: 0m, fundingSource: null),
            NewAction(plan.Id, accountId, "InProgress", budgetAmount: 1m, fundingSource: "LGU"),
            NewAction(plan.Id, accountId, "OnTrack", budgetAmount: 1m, fundingSource: "GAA"),
            NewAction(plan.Id, accountId, "Delayed", budgetAmount: 1m, fundingSource: "Grant"),
            NewAction(plan.Id, accountId, "Completed", budgetAmount: 1m, fundingSource: null),
            NewAction(plan.Id, accountId, "Cancelled", budgetAmount: 0m, fundingSource: "Local"));

        var indEarly = NewIndicator(plan.Id, accountId, "NotStarted");
        var indLate = NewIndicator(plan.Id, accountId, "InProgress");
        db.MonitoringIndicators.AddRange(indEarly, indLate);

        await db.SaveChangesAsync();

        var t0 = new DateTimeOffset(2026, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var t1 = new DateTimeOffset(2026, 2, 20, 0, 0, 0, TimeSpan.Zero);
        db.MonitoringUpdates.AddRange(
            NewUpdate(accountId, indEarly.Id, t0),
            NewUpdate(accountId, indLate.Id, t1));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer));

        var result = await controller.GetOperationalDashboard(plan.Id, null, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dash = Assert.IsType<PlanOperationalDashboardDto>(ok.Value);

        Assert.Equal(6, dash.Actions.TotalActions);
        Assert.Equal(1, dash.Actions.PlannedCount);
        Assert.Equal(1, dash.Actions.InProgressCount);
        Assert.Equal(1, dash.Actions.OnTrackCount);
        Assert.Equal(1, dash.Actions.DelayedCount);
        Assert.Equal(1, dash.Actions.CompletedCount);
        Assert.Equal(1, dash.Actions.CancelledCount);
        Assert.Equal(4, dash.Actions.ActionsWithBudgetCount);
        Assert.Equal(4, dash.Actions.ActionsWithFundingSourceCount);
        Assert.Equal(2, dash.Actions.MissingFundingSourceCount);

        Assert.Equal(2, dash.Monitoring.TotalIndicators);
        Assert.Equal(2, dash.Monitoring.TotalMonitoringUpdates);
        Assert.Equal(2, dash.Monitoring.IndicatorsWithUpdatesCount);
        Assert.Equal(t1, dash.Monitoring.LatestMonitoringUpdateAtUtc);
    }

    [Fact]
    public async Task Operational_dashboard_counts_comments_and_funding_by_currency()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Review funding plan");

        db.SectionComments.AddRange(
            NewComment(plan.Id, accountId, "General", resolved: false),
            NewComment(plan.Id, accountId, "DataGap", resolved: false),
            NewComment(plan.Id, accountId, "Validation", resolved: true),
            NewComment(plan.Id, accountId, "RevisionRequest", resolved: false));

        var action = NewAction(plan.Id, accountId, "Planned");
        db.ActionItems.Add(action);
        var source = NewFundingSource(accountId);
        db.FundingSources.Add(source);
        await db.SaveChangesAsync();

        db.ActionFundingAllocations.AddRange(
            NewAllocation(accountId, plan.Id, action.Id, source.Id, 100m, "PHP", tagId: Guid.NewGuid()),
            NewAllocation(accountId, plan.Id, action.Id, source.Id, 50m, "USD", tagId: null));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Reviewer));

        var result = await controller.GetOperationalDashboard(plan.Id, null, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dash = Assert.IsType<PlanOperationalDashboardDto>(ok.Value);

        Assert.Equal(4, dash.Review.TotalComments);
        Assert.Equal(3, dash.Review.UnresolvedComments);
        Assert.Equal(1, dash.Review.ResolvedComments);
        Assert.Equal(1, dash.Review.DataGapComments);
        Assert.Equal(1, dash.Review.ValidationComments);
        Assert.Equal(1, dash.Review.RevisionRequestComments);

        Assert.Equal(2, dash.Funding.TotalAllocations);
        Assert.Equal(1, dash.Funding.CcetTaggedAllocations);
        Assert.Equal(1, dash.Funding.UntaggedAllocations);
        Assert.Equal(2, dash.Funding.AllocationTotalsByCurrency.Count);
        Assert.Contains(dash.Funding.AllocationTotalsByCurrency, r => r.CurrencyCode == "PHP" && r.TotalAllocatedAmount == 100m);
        Assert.Contains(dash.Funding.AllocationTotalsByCurrency, r => r.CurrencyCode == "USD" && r.TotalAllocatedAmount == 50m);
    }

    [Fact]
    public async Task Map_workspace_returns_not_found_for_cross_tenant_plan()
    {
        using var db = CreateDbContext();
        var ownerAccountId = Guid.NewGuid();
        var requesterAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, ownerAccountId, "Other");
        var controller = CreateController(db, requesterAccountId, userId);

        var result = await controller.GetPlanMapWorkspace(
            plan.Id,
            new GetPlanMapWorkspaceQuery(db, new TestCurrentUserContext(requesterAccountId, userId, true, WorkspaceRoles.Admin)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Map_workspace_excludes_soft_deleted_map_assets_facilities_and_barangays()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Map plan");
        var now = DateTimeOffset.UtcNow;

        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);

        var bActive = NewBarangay(accountId, "Alpha", deleted: false);
        var bGone = NewBarangay(accountId, "GoneTown", deleted: true, now);
        db.Barangays.AddRange(bActive, bGone);

        var cfActive = NewCriticalFacility(accountId, plan.Id, bActive.Id, "Hosp", "Hospital", deleted: false);
        var cfGone = NewCriticalFacility(accountId, plan.Id, null, "Old", "Other", deleted: true, now);
        db.CriticalFacilities.AddRange(cfActive, cfGone);

        var f2 = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(f2);

        var mActive = NewMapAsset(accountId, plan.Id, file.Id, "Layer A", deleted: false);
        var mGone = NewMapAsset(accountId, plan.Id, f2.Id, "Gone Layer", deleted: true, now);
        db.MapAssets.AddRange(mActive, mGone);

        var featOk = NewGeoFeature(accountId, mActive.Id, deleted: false);
        var featGone = NewGeoFeature(accountId, mActive.Id, deleted: true, now);
        db.GeoJsonLayerFeatures.AddRange(featOk, featGone);

        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanMapWorkspaceQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer));

        var result = await controller.GetPlanMapWorkspace(plan.Id, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var ws = Assert.IsType<PlanMapWorkspaceDto>(ok.Value);
        Assert.Single(ws.MapAssets);
        Assert.Equal("Layer A", ws.MapAssets[0].Name);
        Assert.Equal(1, ws.MapAssets[0].FeatureCount);
        Assert.Single(ws.Barangays);
        Assert.Equal("Alpha", ws.Barangays[0].Name);
        Assert.Single(ws.CriticalFacilities);
        Assert.Equal("Hosp", ws.CriticalFacilities[0].Name);
        Assert.Equal(1, ws.Counts.MapAssets);
        Assert.Equal(1, ws.Counts.GeoJsonLayers);
        Assert.Equal(1, ws.Counts.Barangays);
        Assert.Equal(1, ws.Counts.CriticalFacilities);
    }

    [Fact]
    public async Task Map_workspace_excludes_map_assets_when_joined_file_asset_belongs_to_other_tenant()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Map plan");

        var fileOtherTenant = SeedGeoJsonFileAsset(otherAccountId);
        db.FileAssets.Add(fileOtherTenant);

        var badAsset = NewMapAsset(accountId, plan.Id, fileOtherTenant.Id, "Layer A", deleted: false);
        db.MapAssets.Add(badAsset);

        db.GeoJsonLayerFeatures.Add(NewGeoFeature(accountId, badAsset.Id, deleted: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanMapWorkspaceQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetPlanMapWorkspace(plan.Id, q, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var ws = Assert.IsType<PlanMapWorkspaceDto>(ok.Value);

        Assert.Empty(ws.MapAssets);
        Assert.Equal(0, ws.Counts.MapAssets);
        Assert.Equal(0, ws.Counts.GeoJsonLayers);
    }

    [Fact]
    public async Task Map_workspace_returns_correct_feature_counts_for_multiple_map_assets()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Map plan");

        var file1 = SeedGeoJsonFileAsset(accountId);
        var file2 = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.AddRange(file1, file2);
        await db.SaveChangesAsync();

        var asset1 = NewMapAsset(accountId, plan.Id, file1.Id, "Layer 1", deleted: false);
        var asset2 = NewMapAsset(accountId, plan.Id, file2.Id, "Layer 2", deleted: false);
        db.MapAssets.AddRange(asset1, asset2);

        db.GeoJsonLayerFeatures.AddRange(
            NewGeoFeature(accountId, asset1.Id, deleted: false),
            NewGeoFeature(accountId, asset1.Id, deleted: false),
            NewGeoFeature(accountId, asset2.Id, deleted: false),
            NewGeoFeature(accountId, asset2.Id, deleted: false),
            NewGeoFeature(accountId, asset2.Id, deleted: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanMapWorkspaceQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetPlanMapWorkspace(plan.Id, q, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var ws = Assert.IsType<PlanMapWorkspaceDto>(ok.Value);

        Assert.Equal(2, ws.MapAssets.Count);
        Assert.Equal(2, ws.MapAssets.Single(m => m.Id == asset1.Id).FeatureCount);
        Assert.Equal(3, ws.MapAssets.Single(m => m.Id == asset2.Id).FeatureCount);

        Assert.Equal(2, ws.Counts.MapAssets);
        Assert.Equal(2, ws.Counts.GeoJsonLayers);
    }

    [Fact]
    public async Task Map_workspace_response_serializes_without_stored_path()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "P");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        db.MapAssets.Add(NewMapAsset(accountId, plan.Id, file.Id, "L", deleted: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanMapWorkspaceQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.GetPlanMapWorkspace(plan.Id, q, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);

        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("stored_path", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("storedPath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Geojson_layer_post_creates_map_asset_and_features()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Flood zones", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var summary = Assert.IsType<CreatedGeoJsonMapAssetSummaryDto>(created.Value);
        Assert.Equal(1, summary.FeatureCount);

        Assert.Single(await db.MapAssets.Where(m => m.PlanId == plan.Id && !m.IsDeleted).ToListAsync());
        Assert.Single(await db.GeoJsonLayerFeatures.Where(g => !g.IsDeleted).ToListAsync());
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_boolean_is_stored_as_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":true,\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Null(feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_object_is_stored_as_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":{},\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<ObjectResult>(result);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Null(feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_array_is_stored_as_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":[],\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<ObjectResult>(result);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Null(feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_null_is_stored_as_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":null,\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<ObjectResult>(result);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Null(feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_missing_is_stored_as_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Null(feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_string_is_stored_as_value()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Equal("x1", feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_feature_id_number_is_stored_as_raw_numeric_text()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":123,\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Equal("123", feature.FeatureId);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_when_feature_geometry_is_null()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":null}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_when_feature_geometry_missing()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_when_geometry_type_is_unsupported()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Circle\",\"coordinates\":[125,7]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_defaults_feature_style_when_properties_style_is_not_object()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\",\"style\":\"red\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);

        var feature = await db.GeoJsonLayerFeatures.SingleAsync(g => !g.IsDeleted);
        Assert.Equal("{}", feature.StyleJson.RootElement.GetRawText());
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_oversized_default_style_json()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var big = new string('a', 60_000);
        using var defaultStyle = JsonDocument.Parse("{\"style\":\"" + big + "\"}");

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, defaultStyle, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_oversized_bounds_json()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var big = new string('b', 60_000);
        using var bounds = JsonDocument.Parse("{\"bounds\":\"" + big + "\"}");

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" +
            "{\"type\":\"Feature\",\"id\":\"x1\",\"properties\":{\"name\":\"A\"},\"geometry\":{\"type\":\"Point\",\"coordinates\":[125.1,7.2]}}" +
            "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Layer", "Flood", null, geo, null, bounds),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_non_feature_collection()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[0,0]},\"properties\":{}}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "X", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_over_500_features()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        List<string> parts = [];
        for (var i = 0; i < 501; i++)
        {
            var lon = (125m + i * 0.001m).ToString(System.Globalization.CultureInfo.InvariantCulture);
            parts.Add(
                $"{{\"type\":\"Feature\",\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{lon},7]}},\"properties\":{{}}}}");
        }

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[" + string.Join(",", parts) + "]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Big", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_invalid_map_type()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[125,7]},\"properties\":{}}]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(file.Id, "Bad", "NotAType", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Geojson_layer_post_rejects_cross_tenant_file_asset()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var fileOther = SeedGeoJsonFileAsset(otherAccountId);
        db.FileAssets.Add(fileOther);
        await db.SaveChangesAsync();

        using var geo = JsonDocument.Parse(
            "{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"Point\",\"coordinates\":[125,7]},\"properties\":{}}]}");

        var controller = CreateController(db, accountId, userId);
        var cmd = new CreateGeoJsonLayerCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.CreateGeoJsonLayer(
            plan.Id,
            new CreateGeoJsonLayerApiRequest(fileOther.Id, "X", "Flood", null, geo, null, null),
            cmd,
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Map_features_get_returns_only_same_tenant()
    {
        using var db = CreateDbContext();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        var asset = NewMapAsset(accountId, plan.Id, file.Id, "L", deleted: false);
        db.MapAssets.Add(asset);
        db.GeoJsonLayerFeatures.Add(NewGeoFeature(accountId, asset.Id, deleted: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, otherAccountId, userId);
        var q = new GetGeoJsonLayerFeaturesQuery(db, new TestCurrentUserContext(otherAccountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetGeoJsonLayerFeatures(asset.Id, null, q, CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Archive_map_asset_soft_deletes_related_features()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");
        var file = SeedGeoJsonFileAsset(accountId);
        db.FileAssets.Add(file);
        var asset = NewMapAsset(accountId, plan.Id, file.Id, "L", deleted: false);
        db.MapAssets.Add(asset);
        db.GeoJsonLayerFeatures.Add(NewGeoFeature(accountId, asset.Id, deleted: false));
        db.MapAnnotations.Add(NewMapAnnotation(accountId, asset.Id, deleted: false));
        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var cmd = new ArchiveMapAssetCommand(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));
        var result = await controller.ArchiveMapAsset(asset.Id, cmd, CancellationToken.None);
        _ = Assert.IsType<NoContentResult>(result);

        var reloaded = await db.MapAssets.SingleAsync(m => m.Id == asset.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.All(
            await db.GeoJsonLayerFeatures.Where(g => g.MapAssetId == asset.Id).ToListAsync(),
            g => Assert.True(g.IsDeleted));
        Assert.All(
            await db.MapAnnotations.Where(a => a.MapAssetId == asset.Id).ToListAsync(),
            a => Assert.True(a.IsDeleted));
    }

    [Fact]
    public async Task Operational_dashboard_activity_omits_sensitive_audit_fields_and_respects_limit()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Audit Plan");
        var action = NewAction(plan.Id, accountId, "Planned");
        db.ActionItems.Add(action);
        await db.SaveChangesAsync();

        for (var i = 0; i < 20; i++)
        {
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                UserId = userId,
                EntityName = "ActionItem",
                EntityId = action.Id,
                Action = $"TestAction{i}",
                OldValuesJson = JsonDocument.Parse("{\"secret\":\"old\"}"),
                NewValuesJson = JsonDocument.Parse("{\"secret\":\"new\"}"),
                MetadataJson = JsonDocument.Parse("{\"planId\":\"" + plan.Id + "\"}"),
                IpAddress = "10.0.0.1",
                UserAgent = "BadAgent/1.0",
                CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-i),
                RowVersion = new byte[] { 9, 2, 3, 4, 5, 6, 7, 8 },
            };
            db.AuditLogs.Add(audit);
        }

        await db.SaveChangesAsync();

        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetOperationalDashboard(plan.Id, 5, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        Assert.DoesNotContain("ipAddress", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userAgent", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("oldValuesJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("newValuesJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("metadataJson", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", json, StringComparison.Ordinal);

        var dash = Assert.IsType<PlanOperationalDashboardDto>(ok.Value);
        Assert.Equal(5, dash.RecentActivity.Count);
    }

    [Fact]
    public async Task Operational_dashboard_suggested_next_steps_include_expected_gaps()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Empty gaps plan");
        var controller = CreateController(db, accountId, userId);
        var q = new GetPlanOperationalDashboardQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner));

        var result = await controller.GetOperationalDashboard(plan.Id, null, q, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dash = Assert.IsType<PlanOperationalDashboardDto>(ok.Value);

        Assert.Contains("Add official evidence documents.", dash.ExportReadiness.SuggestedNextSteps);
        Assert.Contains("Define action items for this plan.", dash.ExportReadiness.SuggestedNextSteps);
    }

    [Fact]
    public async Task Missing_role_claim_returns_forbidden_for_plans()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, null);

        var result = await controller.GetPlans(
            null, null,
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true, null)),
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public void Planner_role_claim_from_plain_role_claim_is_recognized()
    {
        // This test verifies the CurrentUserContext extraction logic via SetFromPrincipal
        var context = new CurrentUserContext();
        var claims = new List<Claim>
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("role", "Planner")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        context.SetFromPrincipal(principal);

        Assert.Equal(WorkspaceRoles.Planner, context.Role);
    }

    [Fact]
    public void Planner_role_claim_case_insensitive_is_recognized()
    {
        var context = new CurrentUserContext();
        var claims = new List<Claim>
        {
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim("account_id", Guid.NewGuid().ToString()),
            new Claim("role", "planner") // lowercase
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        context.SetFromPrincipal(principal);

        // CurrentUserContext.Role will be "planner"
        Assert.Equal("planner", context.Role);

        // But the policy should recognize it
        Assert.True(WorkspaceAuthorizationPolicy.CanRead(context.Role));
    }

    private static List<PlanListItemDto> AssertPlansList(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var prop = ok.Value.GetType().GetProperty("items") ?? ok.Value.GetType().GetProperty("plans");
        Assert.NotNull(prop);
        var raw = prop.GetValue(ok.Value);
        if (raw is System.Collections.IEnumerable enumerable && !(raw is string))
        {
            return enumerable.Cast<PlanListItemDto>().ToList();
        }
        return Assert.IsType<List<PlanListItemDto>>(raw);
    }

    private static PlansController CreateController(LccapDbContext db, Guid? accountId, Guid userId, string? role = WorkspaceRoles.Admin)
    {
        _ = db;
        return new PlansController(new TestCurrentUserContext(accountId, userId, true, role));
    }

    private static PlansController CreateController(LccapDbContext db, ICurrentUserContext context)
    {
        _ = db;
        return new PlansController(context);
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"plans-tests-{Guid.NewGuid()}")
            .Options;

        return new TestLccapDbContext(options);
    }

    private static async Task<Plan> SeedPlan(
        LccapDbContext db,
        Guid accountId,
        string title,
        bool isDeleted = false,
        DateTimeOffset? createdAtUtc = null,
        DateTimeOffset? updatedAtUtc = null)
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
            CreatedAtUtc = createdAtUtc ?? DateTimeOffset.UtcNow,
            UpdatedAtUtc = updatedAtUtc,
            IsDeleted = isDeleted,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = db.Plans.Add(plan);
        _ = await db.SaveChangesAsync();
        return plan;
    }

    private static FileAsset SeedMinimalFile(Guid accountId)
    {
        return new FileAsset
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            OwnerType = "PlanDocument",
            OwnerId = null,
            OriginalFileName = "evidence.pdf",
            StoredFileName = "evidence.bin",
            StoredPath = "internal/test-path",
            ContentType = "application/pdf",
            FileExtension = ".pdf",
            FileSizeBytes = 11,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static Document NewDocument(
        Guid accountId,
        Guid planId,
        Guid fileAssetId,
        string evidenceStatus,
        Guid? planSectionId,
        Guid? actionItemId)
    {
        var d = new Document
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = fileAssetId,
            Category = "Reference",
            EvidenceStatus = evidenceStatus,
            PlanSectionId = planSectionId,
            ActionItemId = actionItemId,
            TagsJson = JsonDocument.Parse("[]"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        return d;
    }

    private static PlanSection NewPlanSection(Guid accountId, Guid planId)
    {
        var s = new PlanSection
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            SectionKey = "introduction",
            Title = "Introduction",
            Content = string.Empty,
            SortOrder = 20,
            SectionMetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 },
        };
        return s;
    }

    private static ActionItem NewAction(
        Guid planId,
        Guid accountId,
        string status,
        decimal budgetAmount = 0m,
        string? fundingSource = null)
    {
        var a = new ActionItem
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Title = $"Action {status}",
            ActionType = "Adaptation",
            Sector = "Water",
            ResponsibleOffice = null,
            BudgetAmount = budgetAmount,
            FundingSource = fundingSource,
            Status = status,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 3, 2, 3, 4, 5, 6, 7, 8 },
        };
        return a;
    }

    private static MonitoringIndicator NewIndicator(Guid planId, Guid accountId, string status)
    {
        var i = new MonitoringIndicator
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = null,
            Name = $"Indicator {status}",
            Description = null,
            Status = status,
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 4, 2, 3, 4, 5, 6, 7, 8 },
        };
        return i;
    }

    private static MonitoringUpdate NewUpdate(Guid accountId, Guid indicatorId, DateTimeOffset reportedAtUtc)
    {
        var u = new MonitoringUpdate
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            MonitoringIndicatorId = indicatorId,
            PeriodLabel = "Q1",
            Status = "OnTrack",
            ReportedAtUtc = reportedAtUtc,
            CreatedAtUtc = reportedAtUtc,
            IsDeleted = false,
            RowVersion = new byte[] { 5, 2, 3, 4, 5, 6, 7, 8 },
        };
        return u;
    }

    private static SectionComment NewComment(Guid planId, Guid accountId, string commentType, bool resolved)
    {
        return new SectionComment
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            SectionKey = "introduction",
            CommentType = commentType,
            CommentText = "Review text",
            CreatedByUserId = Guid.NewGuid(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsResolved = resolved,
            RowVersion = new byte[] { 6, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static FundingSource NewFundingSource(Guid accountId)
    {
        return new FundingSource
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Test source",
            SourceType = "Grant",
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 7, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static ActionFundingAllocation NewAllocation(
        Guid accountId,
        Guid planId,
        Guid actionId,
        Guid sourceId,
        decimal amount,
        string currency,
        Guid? tagId)
    {
        var a = new ActionFundingAllocation
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = actionId,
            FundingSourceId = sourceId,
            FiscalYear = 2026,
            AllocatedAmount = amount,
            CurrencyCode = currency,
            AllocationStatus = "Planned",
            ClimateExpenditureTagId = tagId,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 8, 2, 3, 4, 5, 6, 7, 8 },
        };
        return a;
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

    private static Barangay NewBarangay(
        Guid accountId,
        string name,
        bool deleted,
        DateTimeOffset? deletedAtUtc = null,
        Guid? deletedByUserId = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new Barangay
        {
            AccountId = accountId,
            Name = name,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = deleted,
            DeletedAtUtc = deleted ? deletedAtUtc ?? now : null,
            DeletedByUserId = deleted ? deletedByUserId ?? Guid.NewGuid() : null,
            RowVersion = new byte[] { 11, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static CriticalFacility NewCriticalFacility(
        Guid accountId,
        Guid planId,
        Guid? barangayId,
        string name,
        string facilityType,
        bool deleted,
        DateTimeOffset? deletedAtUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new CriticalFacility
        {
            AccountId = accountId,
            PlanId = planId,
            BarangayId = barangayId,
            Name = name,
            FacilityType = facilityType,
            IsEvacuationSite = false,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = deleted,
            DeletedAtUtc = deleted ? deletedAtUtc ?? now : null,
            DeletedByUserId = deleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 12, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static MapAsset NewMapAsset(
        Guid accountId,
        Guid planId,
        Guid fileId,
        string name,
        bool deleted,
        DateTimeOffset? deletedAtUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new MapAsset
        {
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = fileId,
            Name = name,
            MapType = "Flood",
            MapFormat = "GeoJson",
            DefaultStyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = deleted,
            DeletedAtUtc = deleted ? deletedAtUtc ?? now : null,
            DeletedByUserId = deleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 13, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static GeoJsonLayerFeature NewGeoFeature(
        Guid accountId,
        Guid mapAssetId,
        bool deleted,
        DateTimeOffset? deletedAtUtc = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new GeoJsonLayerFeature
        {
            AccountId = accountId,
            MapAssetId = mapAssetId,
            FeatureId = Guid.NewGuid().ToString("N"),
            FeatureType = "Point",
            DisplayName = "P",
            PropertiesJson = JsonDocument.Parse("{}"),
            GeometryJson = JsonDocument.Parse("{\"type\":\"Point\",\"coordinates\":[125,7]}"),
            StyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = deleted,
            DeletedAtUtc = deleted ? deletedAtUtc ?? now : null,
            DeletedByUserId = deleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 14, 2, 3, 4, 5, 6, 7, 8 },
        };
    }

    private static MapAnnotation NewMapAnnotation(Guid accountId, Guid mapAssetId, bool deleted)
    {
        var now = DateTimeOffset.UtcNow;
        return new MapAnnotation
        {
            AccountId = accountId,
            MapAssetId = mapAssetId,
            GeometryJson = JsonDocument.Parse("{\"type\":\"Point\",\"coordinates\":[0,0]}"),
            StyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            IsDeleted = deleted,
            RowVersion = new byte[] { 15, 2, 3, 4, 5, 6, 7, 8 },
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
