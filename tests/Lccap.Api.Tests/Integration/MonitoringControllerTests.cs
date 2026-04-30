using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring.Commands;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    public async Task UpdateIndicator_WithinSameAccount_Succeeds()
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
        var command = new UpdateIndicatorCommand();

        var result = await controller.UpdateIndicator(
            indicatorId,
            new MonitoringController.UpdateIndicatorRequest(
                "After",
                "new",
                1m,
                2m,
                "pct",
                "OnTrack",
                currentVersion),
            command,
            dbContext,
            currentUser,
            CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        var updated = await dbContext.MonitoringIndicators.SingleAsync(x => x.Id == indicatorId);
        Assert.Equal("After", updated.Name);
        Assert.Equal("OnTrack", updated.Status);
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

    private static MonitoringController CreateController(out TestCurrentUserContext currentUser)
    {
        currentUser = new TestCurrentUserContext();
        return new MonitoringController();
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new TestLccapDbContext(options);
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

    private sealed class TestLccapDbContext : LccapDbContext
    {
        public TestLccapDbContext(DbContextOptions<LccapDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Account>();
            modelBuilder.Ignore<User>();
            modelBuilder.Ignore<TenantSetting>();
            modelBuilder.Ignore<AuditLog>();
            modelBuilder.Ignore<Role>();
            modelBuilder.Ignore<Permission>();
            modelBuilder.Ignore<UserRole>();
            modelBuilder.Ignore<RolePermission>();
            modelBuilder.Ignore<PlanSection>();
            modelBuilder.Ignore<FileAsset>();
            modelBuilder.Ignore<Document>();
            modelBuilder.Ignore<MonitoringUpdate>();
            modelBuilder.Ignore<ActionItem>();

            _ = modelBuilder.Entity<Plan>(builder =>
            {
                _ = builder.HasKey(e => e.Id);
                _ = builder.Property(e => e.Id);
                _ = builder.Property(e => e.AccountId).IsRequired();
                _ = builder.Property(e => e.Title).IsRequired();
                _ = builder.Property(e => e.StartYear).IsRequired();
                _ = builder.Property(e => e.EndYear).IsRequired();
                _ = builder.Property(e => e.Status).IsRequired();
                _ = builder.Property(e => e.TemplateMode).IsRequired();
                _ = builder.Property(e => e.VersionNumber).IsRequired();
                _ = builder.Property(e => e.IsDeleted).IsRequired();
                _ = builder.Property(e => e.RowVersion).IsConcurrencyToken();
            });

            _ = modelBuilder.Entity<MonitoringIndicator>(builder =>
            {
                _ = builder.HasKey(e => e.Id);
                _ = builder.Property(e => e.RowVersion).IsConcurrencyToken();
            });
        }
    }
}
