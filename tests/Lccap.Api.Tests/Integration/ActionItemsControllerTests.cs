using System.Text.Json;
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        var start = new DateTimeOffset(2027, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var body = ValidCreateBody() with { TimelineStartUtc = start, TimelineEndUtc = end };

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(otherAccount, userId, true);

        var result = await new ActionItemsController().Create(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);
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

        var result = await new ActionItemsController().ListForPlan(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);
        var created = await new CreateActionItemCommand(db, ctx).ExecuteAsync(
            plan.Id,
            ToApp(ValidCreateBody()),
            CancellationToken.None);

        var result = await new ActionItemsController().GetById(
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
        var ownerCtx = new TestCurrentUserContext(ownerAccount, userId, true);
        var created = await new CreateActionItemCommand(db, ownerCtx).ExecuteAsync(
            plan.Id,
            ToApp(ValidCreateBody()),
            CancellationToken.None);

        var ctx = new TestCurrentUserContext(otherAccount, userId, true);

        var result = await new ActionItemsController().GetById(
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
        var ctx = new TestCurrentUserContext(accountId, userId, true);

        await new ActionItemsController().Create(
            plan.Id,
            ValidCreateBody(title: "Before"),
            new CreateActionItemCommand(db, ctx),
            CancellationToken.None);

        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id && a.Title == "Before");

        var result = await new ActionItemsController().Update(
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
        var ownerCtx = new TestCurrentUserContext(ownerAccount, userId, true);
        await new ActionItemsController().Create(
            plan.Id,
            ValidCreateBody(),
            new CreateActionItemCommand(db, ownerCtx),
            CancellationToken.None);

        var persisted = await db.ActionItems.SingleAsync(a => a.PlanId == plan.Id);

        var ctx = new TestCurrentUserContext(otherAccount, userId, true);

        var result = await new ActionItemsController().Update(
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
        public TestCurrentUserContext(Guid accountId, Guid userId, bool isAuthenticated)
        {
            AccountId = accountId;
            UserId = userId;
            IsAuthenticated = isAuthenticated;
        }

        public Guid? AccountId { get; }

        public Guid? UserId { get; }

        public bool IsAuthenticated { get; }
    }
}
