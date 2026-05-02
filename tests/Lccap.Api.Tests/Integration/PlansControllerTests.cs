using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Plans.Commands;
using Lccap.Application.Plans.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new CreatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlanByIdQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlanByIdQuery(db, new TestCurrentUserContext(requesterAccountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
            new GetPlansQuery(db, new TestCurrentUserContext(accountId, userId, true)),
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
        var controller = CreateController(db, Guid.NewGuid(), userId);

        var result = await controller.GetPlans(
            new GetPlansQuery(db, new TestCurrentUserContext(null, userId, true)),
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
            new UpdatePlanCommand(db, new TestCurrentUserContext(accountId, userId, true)),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var updated = await db.Plans.SingleAsync(p => p.Id == seeded.Id);
        Assert.Equal("After", updated.Title);
    }

    [Fact]
    public async Task Cross_tenant_update_returns_404()
    {
        using var db = CreateDbContext();
        var ownerAccountId = Guid.NewGuid();
        var requesterAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var seeded = await SeedPlan(db, ownerAccountId, "Before");
        var controller = CreateController(db, requesterAccountId, userId);

        var result = await controller.UpdatePlan(
            seeded.Id,
            new UpdatePlanApiRequest("After", 2025, 2027, "InProgress", "Enhancement", 2, "updated", null, null, seeded.RowVersion),
            new UpdatePlanCommand(db, new TestCurrentUserContext(requesterAccountId, userId, true)),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    private static List<PlanListItemDto> AssertPlansList(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        var prop = ok.Value.GetType().GetProperty("plans");
        Assert.NotNull(prop);
        var raw = prop.GetValue(ok.Value);
        return Assert.IsType<List<PlanListItemDto>>(raw);
    }

    private static PlansController CreateController(LccapDbContext db, Guid accountId, Guid userId)
    {
        _ = db;
        _ = accountId;
        _ = userId;
        return new PlansController();
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

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(Guid? accountId, Guid? userId, bool isAuthenticated)
        {
            AccountId = accountId;
            UserId = userId;
            IsAuthenticated = isAuthenticated;
        }

        public Guid? UserId { get; }

        public Guid? AccountId { get; }

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
