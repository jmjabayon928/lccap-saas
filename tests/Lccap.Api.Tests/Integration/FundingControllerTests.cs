using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
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
