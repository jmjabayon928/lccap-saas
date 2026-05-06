using System.Text;
using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Lccap.Application.Exports.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class ExportControllerTests
{
    private static readonly DateTimeOffset FixedManifestTime = new(2026, 3, 4, 12, 0, 0, TimeSpan.Zero);

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
        var controller = CreateExportController(dbContext, currentUser, fakeCommand);

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
        var controller = CreateExportController(dbContext, currentUser, fakeCommand);

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
        var controller = CreateExportController(dbContext, currentUser);

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
        var controller = new ExportController(null!, null!, null!, ctx, null!, null!, null!, null!);

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
        var controller = CreateExportController(dbContext, ctx, fakeCommand);

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
        var controller = CreateExportController(dbContext, ctx, fakeCommand);

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
        var controller = CreateExportController(dbContext, ctx, fakeCommand);

        var result = await controller.CreatePdfExport(planId, CancellationToken.None);
        _ = Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task GetExportPackageManifest_cross_tenant_returns_not_found()
    {
        var ownerAccountId = Guid.NewGuid();
        var callerAccountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, ownerAccountId, planId);

        var ctx = new TestCurrentUserContext
        {
            AccountId = callerAccountId,
            UserId = Guid.NewGuid(),
            IsAuthenticated = true,
            Role = WorkspaceRoles.Viewer
        };
        var controller = CreateExportController(dbContext, ctx);
        var result = await controller.GetExportPackageManifest(planId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExportPackageManifest_returns_expected_counts_and_readiness()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId, "LGU Plan Alpha");

        var sentinelPath = $"uploads/secret-export-{Guid.NewGuid():N}.csv";
        var fa = NewFileAsset(accountId, planId, sentinelPath);
        dbContext.FileAssets.Add(fa);

        var actionAlive = NewAction(accountId, planId, "Active action");
        var actionDeleted = NewAction(accountId, planId, "Gone action", isDeleted: true);
        dbContext.ActionItems.AddRange(actionAlive, actionDeleted);
        await dbContext.SaveChangesAsync();

        var indicatorId = Guid.NewGuid();
        var indicator = NewIndicator(accountId, planId, actionAlive.Id, indicatorId);
        dbContext.MonitoringIndicators.Add(indicator);
        await dbContext.SaveChangesAsync();

        dbContext.MonitoringUpdates.Add(
            NewUpdate(accountId, indicatorId, "Q1", 10m, new DateTimeOffset(2025, 10, 1, 8, 0, 0, TimeSpan.Zero), isDeleted: true));
        dbContext.MonitoringUpdates.Add(
            NewUpdate(accountId, indicatorId, "Q2", 20m, new DateTimeOffset(2025, 11, 1, 8, 0, 0, TimeSpan.Zero), isDeleted: false));
        await dbContext.SaveChangesAsync();

        dbContext.Documents.Add(NewDocument(accountId, planId, fa.Id, evidenceStatus: "Official"));
        dbContext.Documents.Add(NewDocument(accountId, planId, fa.Id, evidenceStatus: "Public"));
        dbContext.Documents.Add(NewDocument(accountId, planId, fa.Id, evidenceStatus: "Internal", isDeleted: true));

        dbContext.SectionComments.Add(NewSectionComment(accountId, planId, userId, resolved: false));
        dbContext.SectionComments.Add(NewSectionComment(accountId, planId, userId, resolved: true));

        var source = NewFundingSource(accountId);
        var program = NewFundingProgram(accountId, source.Id, programCode: "PC-A");
        var ccet = NewCcet(accountId, code: "CCET-01");
        dbContext.FundingSources.Add(source);
        dbContext.FundingPrograms.Add(program);
        dbContext.ClimateExpenditureTags.Add(ccet);
        await dbContext.SaveChangesAsync();

        dbContext.ActionFundingAllocations.AddRange(
            NewAllocation(accountId, planId, actionAlive.Id, source.Id, program.Id, ccet.Id, fiscalYear: 2026),
            NewAllocation(accountId, planId, actionAlive.Id, source.Id, null, null, fiscalYear: 2025),
            NewAllocation(accountId, planId, actionAlive.Id, source.Id, null, null, fiscalYear: 2024, isDeleted: true));
        await dbContext.SaveChangesAsync();

        var ctx = ViewerContext(accountId, userId);
        var manifestClock = new FixedUtcClock(FixedManifestTime);
        var controller = new ExportController(
            null!,
            null!,
            dbContext,
            ctx,
            new GetActionMatrixExportQuery(dbContext, ctx),
            new GetMonitoringMatrixExportQuery(dbContext, ctx),
            new GetFundingReadinessExportQuery(dbContext, ctx),
            new GetExportPackageManifestQuery(dbContext, ctx, manifestClock));

        var ok = Assert.IsType<OkObjectResult>(await controller.GetExportPackageManifest(planId, CancellationToken.None));
        var body = Assert.IsType<ExportPackageManifestDto>(ok.Value);
        Assert.Equal(planId, body.PlanId);
        Assert.Equal("LGU Plan Alpha", body.PlanTitle);
        Assert.Equal(FixedManifestTime, body.GeneratedAtUtc);
        Assert.Equal(2, body.Counts.Documents);
        Assert.Equal(1, body.Counts.OfficialEvidence);
        Assert.Equal(1, body.Counts.PublicEvidence);
        Assert.Equal(1, body.Counts.Actions);
        Assert.Equal(1, body.Counts.MonitoringIndicators);
        Assert.Equal(1, body.Counts.MonitoringUpdates);
        Assert.Equal(1, body.Counts.UnresolvedSectionComments);
        Assert.Equal(2, body.Counts.FundingAllocations);
        Assert.Equal(1, body.Counts.CcetTaggedAllocations);
        Assert.True(body.Readiness.HasOfficialEvidence);
        Assert.True(body.Readiness.HasActions);
        Assert.True(body.Readiness.HasMonitoring);
        Assert.True(body.Readiness.HasFundingAllocations);
        Assert.True(body.Readiness.HasUnresolvedComments);
        Assert.EndsWith("/documents/evidence-index.csv", body.AvailableDownloads.EvidenceIndexCsv);
        Assert.Contains(planId.ToString("D"), body.AvailableDownloads.ActionMatrixCsv, StringComparison.Ordinal);

        Assert.IsType<FileContentResult>(await controller.DownloadActionMatrixCsv(planId, CancellationToken.None));

        var monitoringCsv = Encoding.UTF8.GetString(
            Assert.IsType<FileContentResult>(await controller.DownloadMonitoringMatrixCsv(planId, CancellationToken.None)).FileContents);
        Assert.Contains("Q2", monitoringCsv);
        Assert.Contains("20.0000", monitoringCsv);
        Assert.DoesNotContain("Q1", monitoringCsv);

        var fundingCsv = Encoding.UTF8.GetString(
            Assert.IsType<FileContentResult>(await controller.DownloadFundingReadinessCsv(planId, CancellationToken.None)).FileContents);
        Assert.Contains("PC-A", fundingCsv);
        Assert.Contains("CCET-01", fundingCsv);
        Assert.Contains(source.SourceType, fundingCsv);

        var actionCsv = Encoding.UTF8.GetString(
            Assert.IsType<FileContentResult>(await controller.DownloadActionMatrixCsv(planId, CancellationToken.None)).FileContents);
        Assert.Contains("Active action", actionCsv);
        Assert.DoesNotContain("Gone action", actionCsv);
        Assert.DoesNotContain(sentinelPath, actionCsv);
    }

    [Fact]
    public async Task Action_matrix_csv_escapes_injection_and_special_characters()
    {
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        await SeedPlanAsync(dbContext, accountId, planId);

        var formula = NewAction(accountId, planId, "=SUM(1)");
        var quoted = NewAction(accountId, planId, "Say \"Hi\", team");
        var multiline = NewAction(accountId, planId, "Line1\r\nLine2");
        dbContext.ActionItems.AddRange(formula, quoted, multiline);
        await dbContext.SaveChangesAsync();

        var ctx = ViewerContext(accountId, Guid.NewGuid());
        var controller = CreateExportController(dbContext, ctx);
        var file = Assert.IsType<FileContentResult>(await controller.DownloadActionMatrixCsv(planId, CancellationToken.None));
        var text = Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("'=SUM(1)", text, StringComparison.Ordinal);
        Assert.Contains("\"Say \"\"Hi\"", text, StringComparison.Ordinal);
        Assert.Contains("\"Line1", text, StringComparison.Ordinal);
        Assert.DoesNotContain("uploads/", text, StringComparison.Ordinal);
    }

    private static ExportController CreateExportController(
        LccapDbContext db,
        TestCurrentUserContext user,
        CreateExportJobCommand? createExportJob = null,
        IClock? manifestClock = null)
    {
        IClock clock = manifestClock ?? new FixedUtcClock(FixedManifestTime);
        CreateExportJobCommand exportCmd =
            createExportJob
            ?? new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Queued", null));

        return new ExportController(
            exportCmd,
            null!,
            db,
            user,
            new GetActionMatrixExportQuery(db, user),
            new GetMonitoringMatrixExportQuery(db, user),
            new GetFundingReadinessExportQuery(db, user),
            new GetExportPackageManifestQuery(db, user, clock));
    }

    private static TestCurrentUserContext ViewerContext(Guid accountId, Guid userId) =>
        new()
        {
            AccountId = accountId,
            UserId = userId,
            IsAuthenticated = true,
            Role = WorkspaceRoles.Viewer
        };

    private static FileAsset NewFileAsset(Guid accountId, Guid planId, string storedPath)
    {
        var id = Guid.NewGuid();
        var fa = new FileAsset
        {
            Id = id,
            AccountId = accountId,
            OwnerType = "PlanDocument",
            OwnerId = planId,
            OriginalFileName = "seed.bin",
            StoredFileName = "seed.bin",
            StoredPath = storedPath,
            ContentType = "application/octet-stream",
            FileExtension = ".bin",
            FileSizeBytes = 1,
            Sha256Hash = "00",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            MetadataJson = JsonDocument.Parse("{}")
        };
        fa.EnsureRowVersion();
        return fa;
    }

    private static ActionItem NewAction(Guid accountId, Guid planId, string title, bool isDeleted = false)
    {
        var a = new ActionItem
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
            IsDeleted = isDeleted,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        a.EnsureRowVersion();
        return a;
    }

    private static MonitoringIndicator NewIndicator(Guid accountId, Guid planId, Guid actionItemId, Guid indicatorId)
    {
        var m = new MonitoringIndicator
        {
            Id = indicatorId,
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = actionItemId,
            Name = "Indicator A",
            Description = null,
            BaselineValue = 0m,
            TargetValue = 100m,
            Unit = "count",
            Status = "InProgress",
            MetadataJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };
        m.EnsureRowVersion();
        return m;
    }

    private static MonitoringUpdate NewUpdate(
        Guid accountId,
        Guid indicatorId,
        string period,
        decimal? actual,
        DateTimeOffset reportedAt,
        bool isDeleted)
    {
        var u = new MonitoringUpdate
        {
            AccountId = accountId,
            MonitoringIndicatorId = indicatorId,
            PeriodLabel = period,
            ActualValue = actual,
            ProgressPercent = null,
            Status = "InProgress",
            Notes = null,
            ReportedAtUtc = reportedAt,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted,
            RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 }
        };
        u.EnsureRowVersion();
        return u;
    }

    private static Document NewDocument(
        Guid accountId,
        Guid planId,
        Guid fileAssetId,
        string evidenceStatus,
        bool isDeleted = false)
    {
        var d = new Document
        {
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = fileAssetId,
            Category = "Reference",
            Title = $"Doc-{Guid.NewGuid():N}",
            EvidenceStatus = evidenceStatus,
            TagsJson = JsonDocument.Parse("[]"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 9 }
        };
        d.EnsureRowVersion();
        return d;
    }

    private static SectionComment NewSectionComment(Guid accountId, Guid planId, Guid userId, bool resolved)
    {
        var c = new SectionComment
        {
            AccountId = accountId,
            PlanId = planId,
            SectionKey = "intro",
            CommentType = "General",
            CommentText = $"note-{resolved}",
            CreatedByUserId = userId,
            ResolvedAtUtc = resolved ? DateTimeOffset.UtcNow : null,
            ResolvedByUserId = resolved ? userId : null,
            IsResolved = resolved,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 9, 2, 3, 4, 5, 6, 7, 8 }
        };
        c.EnsureRowVersion();
        return c;
    }

    private static FundingSource NewFundingSource(Guid accountId, string sourceType = "LGUInternal")
    {
        var s = new FundingSource
        {
            AccountId = accountId,
            Name = "Source A",
            SourceType = sourceType,
            Description = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 3, 3, 4, 5, 6, 7, 8 }
        };
        s.EnsureRowVersion();
        return s;
    }

    private static FundingProgram NewFundingProgram(Guid accountId, Guid fundingSourceId, string programCode = "P1")
    {
        var p = new FundingProgram
        {
            AccountId = accountId,
            FundingSourceId = fundingSourceId,
            Name = "Program A",
            ProgramCode = programCode,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            Status = "Active",
            RowVersion = new byte[] { 1, 4, 3, 4, 5, 6, 7, 8 }
        };
        p.EnsureRowVersion();
        return p;
    }

    private static ClimateExpenditureTag NewCcet(Guid accountId, string code = "CCET-X")
    {
        var t = new ClimateExpenditureTag
        {
            AccountId = accountId,
            TagCode = code,
            TagName = "Tag",
            TagCategory = "Adaptation",
            WeightPercent = null,
            Description = null,
            IsActive = true,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false
        };
        t.EnsureRowVersion();
        return t;
    }

    private static ActionFundingAllocation NewAllocation(
        Guid accountId,
        Guid planId,
        Guid actionItemId,
        Guid fundingSourceId,
        Guid? programId,
        Guid? ccetId,
        int fiscalYear,
        bool isDeleted = false)
    {
        var a = new ActionFundingAllocation
        {
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = actionItemId,
            FundingSourceId = fundingSourceId,
            FundingProgramId = programId,
            ClimateExpenditureTagId = ccetId,
            FiscalYear = fiscalYear,
            AllocatedAmount = 100m,
            CurrencyCode = "PHP",
            AllocationStatus = "Planned",
            Notes = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = isDeleted
        };
        a.EnsureRowVersion();
        return a;
    }

    private sealed class FixedUtcClock : IClock
    {
        private readonly DateTimeOffset _now;

        public FixedUtcClock(DateTimeOffset now) => _now = now;

        public DateTimeOffset UtcNow => _now;
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"export-tests-{Guid.NewGuid():N}")
            .Options;
        return new ExportControllerTestDbContext(options);
    }

    private static async Task SeedPlanAsync(LccapDbContext dbContext, Guid accountId, Guid planId, string title = "Test Plan")
    {
        dbContext.Plans.Add(new Plan
        {
            Id = planId,
            AccountId = accountId,
            Title = title,
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
