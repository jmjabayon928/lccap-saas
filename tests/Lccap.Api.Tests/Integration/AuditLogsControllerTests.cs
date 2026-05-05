using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Audit.Queries;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Lccap.Api.Tests.Integration;

public sealed class AuditLogsControllerTests
{
    [Fact]
    public async Task Admin_can_list_tenant_audit_logs()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "Plan", "PlanMetadataUpdated");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Plan", pagedResult.Items[0].EntityName);
    }

    [Fact]
    public async Task Reviewer_can_list_tenant_audit_logs()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "Plan", "PlanMetadataUpdated");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Reviewer);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Reviewer));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Planner_cannot_list_audit_logs()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Planner);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Planner));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_list_audit_logs()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Viewer);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Viewer));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Audit_logs_are_tenant_scoped()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var otherAccountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "MyPlan", "Update");
        await SeedAuditLog(db, otherAccountId, userId, "TheirPlan", "Update");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("MyPlan", pagedResult.Items[0].EntityName);
    }

    [Fact]
    public async Task Audit_logs_order_newest_first()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "Old", "Update", DateTimeOffset.UtcNow.AddHours(-1));
        await SeedAuditLog(db, accountId, userId, "New", "Update", DateTimeOffset.UtcNow);
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Equal(2, pagedResult.Items.Count);
        Assert.Equal("New", pagedResult.Items[0].EntityName);
        Assert.Equal("Old", pagedResult.Items[1].EntityName);
    }

    [Fact]
    public async Task Audit_logs_filter_by_entity_name()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "Plan", "Update");
        await SeedAuditLog(db, accountId, userId, "Document", "Update");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs("Plan", null, null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Plan", pagedResult.Items[0].EntityName);
    }

    [Fact]
    public async Task Audit_logs_filter_by_action()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedAuditLog(db, accountId, userId, "Plan", "Created");
        await SeedAuditLog(db, accountId, userId, "Plan", "Updated");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, "Created", null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Single(pagedResult.Items);
        Assert.Equal("Created", pagedResult.Items[0].Action);
    }

    [Fact]
    public async Task Audit_logs_pagination_limits_page_size()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        for (int i = 0; i < 30; i++)
        {
            await SeedAuditLog(db, accountId, userId, $"Plan{i}", "Update");
        }
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 10, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Equal(10, pagedResult.Items.Count);
        Assert.Equal(30, pagedResult.TotalCount);
    }

    [Fact]
    public async Task Audit_logs_include_user_summary_when_available()
    {
        using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            AccountId = accountId,
            Email = "test@lccap.local",
            FullName = "Test User",
            Role = WorkspaceRoles.Admin,
            Status = "Active"
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SeedAuditLog(db, accountId, userId, "Plan", "Update");
        
        var controller = CreateController(db, accountId, userId, WorkspaceRoles.Admin);
        var query = new GetAuditLogsQuery(db, new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin));

        var result = await controller.GetAuditLogs(null, null, null, null, null, null, 1, 25, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<AuditLogPagedResultDto>(ok.Value);
        Assert.Equal("test@lccap.local", pagedResult.Items[0].UserEmail);
        Assert.Equal("Test User", pagedResult.Items[0].UserFullName);
    }

    private static AuditLogsController CreateController(LccapDbContext db, Guid? accountId, Guid userId, string? role)
    {
        return new AuditLogsController(new TestCurrentUserContext(accountId, userId, true, role));
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"audit-logs-tests-{Guid.NewGuid()}")
            .Options;

        return new AuditLogsTestDbContext(options);
    }

    private static async Task SeedAuditLog(LccapDbContext db, Guid accountId, Guid userId, string entityName, string action, DateTimeOffset? createdAt = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            AccountId = accountId,
            UserId = userId,
            EntityName = entityName,
            Action = action,
            CreatedAtUtc = createdAt ?? DateTimeOffset.UtcNow,
            MetadataJson = JsonDocument.Parse("{}")
        });
        await db.SaveChangesAsync();
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(Guid? accountId, Guid? userId, bool isAuthenticated, string? role)
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

    private sealed class AuditLogsTestDbContext : LccapDbContext
    {
        public AuditLogsTestDbContext(DbContextOptions<LccapDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var jsonConverter = new ValueConverter<JsonDocument?, string?>(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, default));

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
