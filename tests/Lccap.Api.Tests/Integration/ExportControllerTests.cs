using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class ExportControllerTests
{
    [Fact]
    public async Task CreatePdfExport_WithValidPlan_ReturnsCreated()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var currentUser = new TestCurrentUserContext
        {
            AccountId = accountId,
            UserId = Guid.NewGuid(),
            IsAuthenticated = true,
            Role = WorkspaceRoles.Admin
        };

        var fakeCommand = new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Queued", null));
        var controller = new ExportController(fakeCommand, null!, dbContext, currentUser);

        var result = await controller.CreatePdfExport(
            planId,
            CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        var body = Assert.IsType<ExportJobResponse>(created.Value);
        Assert.Equal("Queued", body.Status);
    }

    [Fact]
    public async Task CreatePdfExport_CrossTenant_ReturnsNotFound()
    {
        var ownerAccountId = Guid.NewGuid();
        var callerAccountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, ownerAccountId, planId);

        var currentUser = new TestCurrentUserContext
        {
            AccountId = callerAccountId,
            UserId = Guid.NewGuid(),
            IsAuthenticated = true,
            Role = WorkspaceRoles.Admin
        };

        var fakeCommand = new FakeCreateExportJobCommand(CreateExportJobResult.NotFoundError("Plan not found."));
        var controller = new ExportController(fakeCommand, null!, dbContext, currentUser);

        var result = await controller.CreatePdfExport(
            planId,
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetExportJob_ReturnsJobStatus()
    {
        var accountId = Guid.NewGuid();
        var jobId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        dbContext.ExportJobs.Add(new ExportJob
        {
            Id = jobId,
            AccountId = accountId,
            PlanId = Guid.NewGuid(),
            ExportType = "Pdf",
            Status = "Completed",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        });
        await dbContext.SaveChangesAsync();

        var currentUser = new TestCurrentUserContext
        {
            AccountId = accountId,
            UserId = Guid.NewGuid(),
            IsAuthenticated = true,
            Role = WorkspaceRoles.Admin
        };
        var controller = new ExportController(null!, null!, dbContext, currentUser);

        var result = await controller.GetExportJob(
            jobId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<ExportJobResponse>(ok.Value);
        Assert.Equal("Completed", body.Status);
    }

    [Fact]
    public async Task Viewer_cannot_create_export()
    {
        var ctx = new TestCurrentUserContext { IsAuthenticated = true, Role = WorkspaceRoles.Viewer };
        var controller = new ExportController(null!, null!, null!, ctx);

        var result = await controller.CreatePdfExport(Guid.NewGuid(), CancellationToken.None);
        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Reviewer_can_create_export()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Reviewer };

        var fakeCommand = new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Queued", null));
        var controller = new ExportController(fakeCommand, null!, dbContext, ctx);

        var result = await controller.CreatePdfExport(planId, CancellationToken.None);
        _ = Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task Planner_can_create_export()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Planner };

        var fakeCommand = new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Queued", null));
        var controller = new ExportController(fakeCommand, null!, dbContext, ctx);

        var result = await controller.CreatePdfExport(planId, CancellationToken.None);
        _ = Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task Admin_can_create_export()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var ctx = new TestCurrentUserContext { AccountId = accountId, UserId = Guid.NewGuid(), IsAuthenticated = true, Role = WorkspaceRoles.Admin };

        var fakeCommand = new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Queued", null));
        var controller = new ExportController(fakeCommand, null!, dbContext, ctx);

        var result = await controller.CreatePdfExport(planId, CancellationToken.None);
        _ = Assert.IsType<CreatedResult>(result);
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"export-tests-{Guid.NewGuid():N}")
            .Options;
        return new ExportControllerTestDbContext(options);
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

    private sealed class FakeCreateExportJobCommand : CreateExportJobCommand
    {
        private readonly CreateExportJobResult _result;

        public FakeCreateExportJobCommand(CreateExportJobResult result)
            : base(null!, null!, null!)
        {
            _result = result;
        }

        public override Task<CreateExportJobResult> ExecuteAsync(CreateExportJobRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class ExportControllerTestDbContext : LccapDbContext
    {
        public ExportControllerTestDbContext(DbContextOptions<LccapDbContext> options)
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
