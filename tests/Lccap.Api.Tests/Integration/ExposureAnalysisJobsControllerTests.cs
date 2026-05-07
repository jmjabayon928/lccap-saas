using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Commands;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Application.ExposureAnalysisJobs.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.ExposureAnalysisJobs.Python;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace Lccap.Api.Tests.Integration;

public sealed class ExposureAnalysisJobsControllerTests
{
    [Fact]
    public async Task Create_exposure_analysis_job_creates_queued_job_for_same_tenant_active_hazard_layer()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            plan.Id,
            new CreateExposureAnalysisJobRequest(hazard.Id),
            command,
            CancellationToken.None);

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(created.Value);
        Assert.Equal("Queued", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);

        var saved = await db.ExposureAnalysisJobs.SingleAsync(
            j => j.Id == dto.Id && j.AccountId == accountId && j.PlanId == plan.Id && !j.IsDeleted,
            CancellationToken.None);

        Assert.Equal("Queued", saved.Status);
        Assert.Null(saved.OutputJson);
        Assert.Null(saved.ErrorMessage);
        Assert.Null(saved.StartedAtUtc);
        Assert.Null(saved.CompletedAtUtc);
    }

    [Fact]
    public async Task Create_exposure_analysis_job_rejects_cross_tenant_plan_without_leaking_existence()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var otherPlan = await SeedPlan(db, Guid.NewGuid(), "Other plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, Guid.NewGuid());

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            otherPlan.Id,
            new CreateExposureAnalysisJobRequest(hazard.Id),
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_exposure_analysis_job_rejects_cross_tenant_hazard_layer_without_leaking_existence()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazardOtherTenant = await SeedActiveHazardLayer(db, Guid.NewGuid(), plan.Id);

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            plan.Id,
            new CreateExposureAnalysisJobRequest(hazardOtherTenant.Id),
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_exposure_analysis_job_rejects_inactive_hazard_layer()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        hazard.Deactivate();
        _ = db.HazardLayers.Update(hazard);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            plan.Id,
            new CreateExposureAnalysisJobRequest(hazard.Id),
            command,
            CancellationToken.None);

        var bad = Assert.IsType<NotFoundResult>(result);
        Assert.NotNull(bad);
    }

    [Fact]
    public async Task Create_exposure_analysis_job_rejects_soft_deleted_hazard_layer()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        hazard.Archive(Guid.NewGuid(), DateTimeOffset.UtcNow);
        _ = db.HazardLayers.Update(hazard);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            plan.Id,
            new CreateExposureAnalysisJobRequest(hazard.Id),
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_exposure_analysis_job_rejects_duplicate_queued_or_running_job_for_same_hazard_layer()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        _ = db.ExposureAnalysisJobs.Add(new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedByUserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var command = new CreateExposureAnalysisJobCommand(db, currentUser);

        var result = await controller.CreateExposureAnalysisJob(
            plan.Id,
            new CreateExposureAnalysisJobRequest(hazard.Id),
            command,
            CancellationToken.None);

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task Get_exposure_analysis_jobs_returns_only_current_tenant_plan_non_deleted_jobs()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var otherPlan = await SeedPlan(db, Guid.NewGuid(), "Other plan");

        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        _ = db.ExposureAnalysisJobs.Add(new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        _ = db.ExposureAnalysisJobs.Add(new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Completed",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        _ = db.ExposureAnalysisJobs.Add(new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            PlanId = otherPlan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var query = new GetPlanExposureAnalysisJobsQuery(db, currentUser);
        var result = await controller.GetPlanExposureAnalysisJobs(plan.Id, query, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var itemsProp = ok.Value!.GetType().GetProperty("items");
        Assert.NotNull(itemsProp);
        var items = Assert.IsAssignableFrom<IReadOnlyList<ExposureAnalysisJobDto>>(itemsProp!.GetValue(ok.Value)!);
        Assert.Single(items);
        Assert.Equal("Queued", items[0].Status);
    }

    [Fact]
    public async Task Get_exposure_analysis_job_returns_not_found_for_cross_tenant_or_deleted_job()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var plan = await SeedPlan(db, accountId, "Plan");

        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = true,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var query = new GetExposureAnalysisJobQuery(db, currentUser);

        var result = await controller.GetExposureAnalysisJob(plan.Id, job.Id, query, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Process_exposure_analysis_job_uses_not_configured_computation_client_when_python_enabled_missing()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions());
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal("Exposure computation engine is not configured.", dto.ErrorMessage);
        Assert.NotNull(dto.StartedAtUtc);
        Assert.NotNull(dto.CompletedAtUtc);

        var saved = await db.ExposureAnalysisJobs.SingleAsync(
            j => j.Id == job.Id && j.AccountId == accountId && j.PlanId == plan.Id && !j.IsDeleted,
            CancellationToken.None);

        Assert.Equal("Failed", saved.Status);
        Assert.NotNull(saved.StartedAtUtc);
        Assert.NotNull(saved.CompletedAtUtc);
        Assert.Equal("Exposure computation engine is not configured.", saved.ErrorMessage);

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
        Assert.Equal(0, pythonAdapter.CallCount);
    }

    [Fact]
    public async Task Process_exposure_analysis_job_fails_when_hazard_features_are_missing_before_engine_call()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);
        Assert.Equal("Failed", dto.Status);
        Assert.Equal("Exposure computation request could not be prepared.", dto.ErrorMessage);
        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
        Assert.Equal(0, pythonAdapter.CallCount);
    }

    [Fact]
    public async Task Process_exposure_analysis_job_uses_not_configured_computation_client_when_python_enabled_false()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = false });
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal("Exposure computation engine is not configured.", dto.ErrorMessage);
        Assert.NotNull(dto.StartedAtUtc);
        Assert.NotNull(dto.CompletedAtUtc);

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
        Assert.Equal(0, pythonAdapter.CallCount);
    }

    [Fact]
    public async Task Process_exposure_analysis_job_uses_python_adapter_when_python_enabled_and_python_engine_unavailable()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal("Exposure computation engine is not configured.", dto.ErrorMessage);
        Assert.Equal(1, pythonAdapter.CallCount);

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_enabled_python_scaffold_posts_to_compute_exposure_and_handles_engine_unavailable()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);

        var completedAtUtc = DateTimeOffset.UtcNow;
        var scaffoldErrorResponseJson = $@"{{
  ""success"": false,
  ""engineName"": ""ExposureComputationScaffold"",
  ""engineVersion"": ""scaffold"",
  ""computationRunId"": null,
  ""completedAtUtc"": ""{completedAtUtc:O}"",
  ""errorCode"": ""EngineUnavailable"",
  ""errorMessage"": ""Exposure computation engine is not configured."",
  ""diagnostics"": {{
    ""message"": ""Computation endpoint is scaffolded only."",
    ""warnings"": [],
    ""validationNotes"": [],
    ""geometryFeatureCount"": null,
    ""barangayCount"": null,
    ""criticalFacilityCount"": null,
    ""crsDescription"": null
  }},
  ""results"": []
}}";

        var httpHandler = new RecordingHttpMessageHandler(
            (_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(scaffoldErrorResponseJson, Encoding.UTF8, "application/json")
                    }));

        var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var pythonClientOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationOptions
        {
            ComputePath = "/compute/exposure",
            TimeoutSeconds = 30
        });

        var pythonClient = new PythonExposureComputationServiceClient(httpClient, pythonClientOptions);
        var pythonAdapter = new PythonExposureComputationClientAdapter(pythonClient);
        var pythonFeatureOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });

        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonFeatureOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal("Exposure computation engine is not configured.", dto.ErrorMessage);

        Assert.Equal(1, httpHandler.CallCount);
        Assert.NotNull(httpHandler.LastRequest);
        Assert.NotNull(httpHandler.LastRequestBody);

        Assert.Equal(HttpMethod.Post, httpHandler.LastRequest!.Method);
        Assert.Equal("/compute/exposure", httpHandler.LastRequest!.RequestUri!.AbsolutePath);

        using var requestJson = JsonDocument.Parse(httpHandler.LastRequestBody!);
        var root = requestJson.RootElement;

        Assert.True(root.TryGetProperty("jobId", out _), "jobId missing from posted request.");
        Assert.True(root.TryGetProperty("accountId", out _), "accountId missing from posted request.");
        Assert.True(root.TryGetProperty("planId", out _), "planId missing from posted request.");
        Assert.True(root.TryGetProperty("hazardLayerId", out _), "hazardLayerId missing from posted request.");
        Assert.True(root.TryGetProperty("crsPolicy", out _), "crsPolicy missing from posted request.");
        Assert.True(root.TryGetProperty("geometryPolicy", out _), "geometryPolicy missing from posted request.");
        Assert.True(root.TryGetProperty("hazardLayer", out _), "hazardLayer missing from posted request.");
        Assert.True(root.TryGetProperty("hazardFeatures", out _), "hazardFeatures missing from posted request.");
        Assert.True(root.TryGetProperty("barangays", out _), "barangays missing from posted request.");
        Assert.True(root.TryGetProperty("criticalFacilities", out _), "criticalFacilities missing from posted request.");

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_uses_python_adapter_when_python_enabled_and_python_validation_failed()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation service returned an invalid response.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal("Exposure computation service returned an invalid response.", dto.ErrorMessage);
        Assert.Equal(1, pythonAdapter.CallCount);

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_uses_python_adapter_when_python_enabled_and_python_success_marks_failed_persistence_not_implemented()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Succeeded(
                engineVersion: "stub",
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal(
            "Exposure computation succeeded, but exposure summary persistence is not implemented yet.",
            dto.ErrorMessage);
        Assert.Equal(1, pythonAdapter.CallCount);

        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_enabled_python_success_result_marks_failed_persistence_not_implemented_and_creates_no_summaries()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);

        var completedAtUtc = DateTimeOffset.UtcNow;
        var successResponseJson = $@"{{
  ""success"": true,
  ""engineName"": ""FacilityExposureEngine"",
  ""engineVersion"": ""facility-v1"",
  ""computationRunId"": null,
  ""completedAtUtc"": ""{completedAtUtc:O}"",
  ""errorCode"": null,
  ""errorMessage"": null,
  ""diagnostics"": {{
    ""message"": ""Facility-only point-in-polygon computation completed."",
    ""warnings"": [],
    ""validationNotes"": [""exposedAreaHectares, exposedPopulation, and riskScore are deferred.""],
    ""geometryFeatureCount"": 1,
    ""barangayCount"": 1,
    ""criticalFacilityCount"": 1,
    ""crsDescription"": ""EPSG:4326 (Explicit)""
  }},
  ""results"": [
    {{
      ""barangayId"": ""00000000-0000-0000-0000-000000000001"",
      ""criticalFacilityId"": ""00000000-0000-0000-0000-000000000002"",
      ""hazardLayerId"": ""00000000-0000-0000-0000-000000000003"",
      ""hazardType"": ""Flood"",
      ""severity"": ""High"",
      ""exposedAreaHectares"": null,
      ""exposedFacilityCount"": 1,
      ""exposedPopulation"": null,
      ""riskScore"": null,
      ""summaryJson"": {{
        ""mode"": ""FacilityOnlyPointInPolygon"",
        ""boundaryPolicy"": ""BoundaryInclusive"",
        ""matchedHazardFeatureIds"": [""hazard-1""]
      }}
    }}
  ]
}}";

        var httpHandler = new RecordingHttpMessageHandler(
            (_, _) =>
                Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(successResponseJson, Encoding.UTF8, "application/json")
                    }));

        var httpClient = new HttpClient(httpHandler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        var pythonClientOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationOptions
        {
            ComputePath = "/compute/exposure",
            TimeoutSeconds = 30
        });

        var pythonClient = new PythonExposureComputationServiceClient(httpClient, pythonClientOptions);
        var pythonAdapter = new PythonExposureComputationClientAdapter(pythonClient);
        var pythonFeatureOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions { Enabled = true });

        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonFeatureOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);

        Assert.Equal("Failed", dto.Status);
        Assert.Equal(hazard.Id, dto.HazardLayerId);
        Assert.Equal(
            "Exposure computation succeeded, but exposure summary persistence is not implemented yet.",
            dto.ErrorMessage);

        Assert.Equal(1, httpHandler.CallCount);
        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_fails_when_barangay_boundaries_are_missing_before_engine_call()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithoutBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions());
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);
        Assert.Equal("Failed", dto.Status);
        Assert.Equal("Exposure computation request could not be prepared.", dto.ErrorMessage);
        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_fails_when_critical_facility_coordinates_are_missing_before_engine_call()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);
        await SeedGeoJsonLayerFeature(db, accountId, hazard.MapAssetId!.Value);
        await SeedBarangayWithBoundaryGeoJson(db, accountId);
        await SeedCriticalFacility(db, accountId, plan.Id, withCoordinates: false);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions());
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ExposureAnalysisJobDto>(ok.Value);
        Assert.Equal("Failed", dto.Status);
        Assert.Equal("Exposure computation request could not be prepared.", dto.ErrorMessage);
        Assert.False(await db.ExposureSummaries.AnyAsync(s => !s.IsDeleted, CancellationToken.None));
    }

    [Fact]
    public async Task Process_exposure_analysis_job_returns_not_found_for_cross_tenant_job()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        var otherTenantJob = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            PlanId = plan.Id,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(otherTenantJob);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions());
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            otherTenantJob.Id,
            command,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Process_exposure_analysis_job_rejects_non_queued_job()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var plan = await SeedPlan(db, accountId, "Plan");
        var hazard = await SeedActiveHazardLayer(db, accountId, plan.Id);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            Status = "Running",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazard.Id}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{userId}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.ExposureAnalysisJobs.Add(job);
        await db.SaveChangesAsync();

        var (controller, currentUser) = CreateController(accountId, userId, role: WorkspaceRoles.Admin);
        var pythonAdapter = new StubPythonExposureComputationClientAdapter(
            ExposureComputationResult.Failed(
                "Exposure computation engine is not configured.",
                engineName: "PythonStub",
                engineVersion: null,
                completedAtUtc: DateTimeOffset.UtcNow));
        var pythonOptions = Microsoft.Extensions.Options.Options.Create(new PythonExposureComputationFeatureOptions());
        var command = new ProcessExposureAnalysisJobCommand(
            db,
            currentUser,
            new NotConfiguredExposureComputationClient(),
            new ExposureComputationRequestBuilder(db),
            pythonAdapter,
            pythonOptions);

        var result = await controller.ProcessExposureAnalysisJob(
            plan.Id,
            job.Id,
            command,
            CancellationToken.None);

        Assert.IsType<ConflictResult>(result);
    }

    private sealed class StubPythonExposureComputationClientAdapter : IPythonExposureComputationClientAdapter
    {
        private readonly ExposureComputationResult _result;

        public StubPythonExposureComputationClientAdapter(ExposureComputationResult result)
        {
            _result = result;
        }

        public int CallCount { get; private set; }

        public Task<ExposureComputationResult> ExecuteAsync(
            ExposureComputationServiceRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public int CallCount { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastRequestBody { get; private set; }

        public RecordingHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = request;
            LastRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            return await _handler(request, cancellationToken).ConfigureAwait(false);
        }
    }

    private static (ExposureAnalysisJobsController Controller, TestCurrentUserContext CurrentUser) CreateController(
        Guid accountId,
        Guid userId,
        string role)
    {
        var currentUser = new TestCurrentUserContext(accountId, userId, true, role);
        var controller = new ExposureAnalysisJobsController(currentUser);
        return (controller, currentUser);
    }

    private static async Task<Plan> SeedPlan(LccapDbContext db, Guid accountId, string title, bool isDeleted = false)
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
            IsDeleted = isDeleted,
            DeletedAtUtc = isDeleted ? DateTimeOffset.UtcNow : null,
            DeletedByUserId = isDeleted ? Guid.NewGuid() : null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        _ = db.Plans.Add(plan);
        await db.SaveChangesAsync();
        return plan;
    }

    private static async Task<HazardLayer> SeedActiveHazardLayer(LccapDbContext db, Guid accountId, Guid planId)
    {
        // Hazard registration relies on MapAsset and FileAsset; this test seeds enough of the entity graph
        // for the CreateExposureAnalysisJobCommand to validate hazard existence.
        var file = new FileAsset
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
            RowVersion = new byte[] { 9, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.FileAssets.Add(file);
        await db.SaveChangesAsync();

        var mapAsset = new MapAsset
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = file.Id,
            Name = "Layer",
            MapType = "Hazard",
            MapFormat = "GeoJson",
            DefaultStyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 13, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.MapAssets.Add(mapAsset);
        await db.SaveChangesAsync();

        var hazard = new HazardLayer
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            MapAssetId = mapAsset.Id,
            Name = "Haz 1",
            HazardType = "River",
            Severity = "High",
            Source = "Source A",
            Description = null,
            GeometryId = null,
            MetadataJson = JsonDocument.Parse("{}"),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 16, 2, 3, 4, 5, 6, 7, 8 }
        };
        _ = db.HazardLayers.Add(hazard);
        await db.SaveChangesAsync();
        return hazard;
    }

    private static async Task SeedGeoJsonLayerFeature(LccapDbContext db, Guid accountId, Guid mapAssetId)
    {
        _ = db.GeoJsonLayerFeatures.Add(new GeoJsonLayerFeature
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            MapAssetId = mapAssetId,
            FeatureId = "feature-1",
            FeatureType = null,
            DisplayName = null,
            PropertiesJson = JsonDocument.Parse("{\"haz\":true}"),
            GeometryJson = JsonDocument.Parse("{\"type\":\"Point\",\"coordinates\":[0,0]}"),
            StyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedBarangayWithBoundaryGeoJson(LccapDbContext db, Guid accountId)
    {
        _ = db.Barangays.Add(new Barangay
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Barangay",
            Code = null,
            Latitude = 10m,
            Longitude = 120m,
            LandAreaHectares = null,
            Population = 100,
            Households = null,
            Classification = null,
            BoundaryGeoJson = JsonDocument.Parse("{\"type\":\"Polygon\",\"coordinates\":[]}"),
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedBarangayWithoutBoundaryGeoJson(LccapDbContext db, Guid accountId)
    {
        _ = db.Barangays.Add(new Barangay
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Barangay",
            Code = null,
            Latitude = 10m,
            Longitude = 120m,
            LandAreaHectares = null,
            Population = 100,
            Households = null,
            Classification = null,
            BoundaryGeoJson = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedCriticalFacility(
        LccapDbContext db,
        Guid accountId,
        Guid planId,
        bool withCoordinates)
    {
        _ = db.CriticalFacilities.Add(new CriticalFacility
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            BarangayId = null,
            Name = "Facility",
            FacilityType = "Hospital",
            Latitude = withCoordinates ? 10m : null,
            Longitude = withCoordinates ? 120m : null,
            Description = null,
            Capacity = null,
            IsEvacuationSite = false,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 }
        });

        await db.SaveChangesAsync();
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"exposure-analysis-jobs-tests-{Guid.NewGuid()}")
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

