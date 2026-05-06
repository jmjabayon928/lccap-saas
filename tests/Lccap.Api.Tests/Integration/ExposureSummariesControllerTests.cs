using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Application.ExposureSummaries.Dtos;
using Lccap.Application.ExposureSummaries.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class ExposureSummariesControllerTests
{
    [Fact]
    public async Task Get_plan_exposure_summaries_returns_only_current_tenant_plan_non_deleted_summaries()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        _ = await SeedExposureSummary(db, accountId, plan.Id, jobId: null, isDeleted: false);
        _ = await SeedExposureSummary(db, accountId, plan.Id, jobId: null, isDeleted: true);

        var otherPlan = await SeedPlan(db, Guid.NewGuid(), "Other plan");
        _ = await SeedExposureSummary(db, Guid.NewGuid(), otherPlan.Id, jobId: null, isDeleted: false);

        var (controller, currentUser) = CreateController(accountId, userId, WorkspaceRoles.Admin);
        var query = new GetPlanExposureSummariesQuery(db, currentUser);

        var result = await controller.GetPlanExposureSummaries(plan.Id, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var itemsProp = ok.Value!.GetType().GetProperty("items");
        Assert.NotNull(itemsProp);

        var items = Assert.IsAssignableFrom<IReadOnlyList<ExposureSummaryDto>>(itemsProp!.GetValue(ok.Value)!);
        Assert.Single(items);
    }

    [Fact]
    public async Task Get_job_exposure_summaries_returns_only_accessible_job_summaries()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var job = await SeedExposureAnalysisJob(db, accountId, plan.Id);
        var otherJob = await SeedExposureAnalysisJob(db, accountId, plan.Id);

        _ = await SeedExposureSummary(db, accountId, plan.Id, job.Id, isDeleted: false);
        _ = await SeedExposureSummary(db, accountId, plan.Id, otherJob.Id, isDeleted: false);
        _ = await SeedExposureSummary(db, accountId, plan.Id, job.Id, isDeleted: true);

        var (controller, currentUser) = CreateController(accountId, userId, WorkspaceRoles.Admin);
        var query = new GetJobExposureSummariesQuery(db, currentUser);

        var result = await controller.GetJobExposureSummaries(plan.Id, job.Id, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var itemsProp = ok.Value!.GetType().GetProperty("items");
        Assert.NotNull(itemsProp);

        var items = Assert.IsAssignableFrom<IReadOnlyList<ExposureSummaryDto>>(itemsProp!.GetValue(ok.Value)!);
        Assert.Single(items);
        Assert.Equal(job.Id, items[0].ExposureAnalysisJobId);
    }

    [Fact]
    public async Task Get_job_exposure_summaries_returns_not_found_for_cross_tenant_job()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var crossTenantJob = await SeedExposureAnalysisJob(db, Guid.NewGuid(), plan.Id);

        var (controller, currentUser) = CreateController(accountId, userId, WorkspaceRoles.Admin);
        var query = new GetJobExposureSummariesQuery(db, currentUser);

        var result = await controller.GetJobExposureSummaries(plan.Id, crossTenantJob.Id, query, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Get_exposure_summary_returns_single_current_tenant_non_deleted_summary()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var summary = await SeedExposureSummary(db, accountId, plan.Id, jobId: null, isDeleted: false);

        var (controller, currentUser) = CreateController(accountId, userId, WorkspaceRoles.Admin);
        var query = new GetExposureSummaryQuery(db, currentUser);

        var result = await controller.GetExposureSummary(plan.Id, summary.Id, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureSummaryDto>(ok.Value);
        Assert.Equal(summary.Id, dto.Id);
        Assert.Equal(summary.PlanId, dto.PlanId);
        Assert.Equal(summary.HazardLayerId, dto.HazardLayerId);
    }

    [Fact]
    public async Task Get_exposure_summary_returns_not_found_for_cross_tenant_or_deleted_summary()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");

        var deletedSummary = await SeedExposureSummary(db, accountId, plan.Id, jobId: null, isDeleted: true);
        var crossTenantPlan = await SeedPlan(db, Guid.NewGuid(), "Other");
        var crossTenantSummary = await SeedExposureSummary(db, Guid.NewGuid(), crossTenantPlan.Id, jobId: null, isDeleted: false);

        var (controller, currentUser) = CreateController(accountId, userId, WorkspaceRoles.Admin);
        var query = new GetExposureSummaryQuery(db, currentUser);

        var result1 = await controller.GetExposureSummary(plan.Id, deletedSummary.Id, query, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result1);

        // Try to fetch cross-tenant summary using this planId route.
        var result2 = await controller.GetExposureSummary(plan.Id, crossTenantSummary.Id, query, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result2);
    }

    private static (ExposureSummariesController Controller, TestCurrentUserContext CurrentUser) CreateController(
        Guid accountId,
        Guid userId,
        string role)
    {
        var currentUser = new TestCurrentUserContext(accountId, userId, true, role);
        var controller = new ExposureSummariesController(currentUser);
        return (controller, currentUser);
    }

    private static async Task<Plan> SeedPlan(LccapDbContext db, Guid accountId, string title)
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Title = title,
            StartYear = 2025,
            EndYear = 2026,
            Status = "Draft",
            TemplateMode = "New",
            VersionNumber = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        _ = db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static async Task<ExposureAnalysisJob> SeedExposureAnalysisJob(
        LccapDbContext db,
        Guid accountId,
        Guid planId)
    {
        var now = DateTimeOffset.UtcNow;
        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{Guid.NewGuid()}"",""requestedAtUtc"":""{now:O}"",""requestedByUserId"":""{Guid.NewGuid()}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedByUserId = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 9, 2, 3, 4, 5, 6, 7, 8 }
        };

        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();
        return job;
    }

    private static async Task<ExposureSummary> SeedExposureSummary(
        LccapDbContext db,
        Guid accountId,
        Guid planId,
        Guid? jobId,
        bool isDeleted)
    {
        var now = DateTimeOffset.UtcNow;

        var summary = new ExposureSummary
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            ExposureAnalysisJobId = jobId,
            BarangayId = null,
            CriticalFacilityId = null,
            HazardLayerId = null,
            HazardType = "River",
            Severity = "High",
            ExposedAreaHectares = 1.5m,
            ExposedFacilityCount = 0,
            ExposedPopulation = null,
            RiskScore = 0.1m,
            SummaryJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? now : null,
            DeletedByUserId = isDeleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        _ = db.ExposureSummaries.Add(summary);
        await db.SaveChangesAsync();
        return summary;
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"exposure-summaries-tests-{Guid.NewGuid()}")
            .Options;

        return new TestLccapDbContext(options);
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

