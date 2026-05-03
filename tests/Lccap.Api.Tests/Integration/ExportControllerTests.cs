using System.Collections;
using System.Linq.Expressions;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Models;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Lccap.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Lccap.Api.Tests.Integration;

public sealed class ExportControllerTests
{
    [Fact]
    public async Task ValidPdfExport_SucceedsForCurrentAccountPlan()
    {
        var jobId = Guid.NewGuid();
        var fileAssetId = Guid.NewGuid();
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(jobId, "Completed", fileAssetId)),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.CreatePdfExport(Guid.NewGuid(), CancellationToken.None);

        var created = Assert.IsType<CreatedResult>(result);
        var payload = Assert.IsType<ExportJobResponse>(created.Value);
        Assert.Equal("Completed", payload.Status);
        Assert.Equal(fileAssetId, payload.FileAssetId);
        Assert.Equal(jobId, payload.ExportJobId);
    }

    [Fact]
    public async Task CrossTenantPlanExport_Returns404()
    {
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.NotFoundError("Plan not found.")),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.CreatePdfExport(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CrossTenantExportJobRead_Returns404()
    {
        var currentAccountId = Guid.NewGuid();
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(
                exportJobs:
                [
                    new ExportJob
                    {
                        Id = Guid.NewGuid(),
                        AccountId = Guid.NewGuid(),
                        PlanId = Guid.NewGuid(),
                        ExportType = "Pdf",
                        Status = "Completed",
                        IsDeleted = false,
                    },
                ]),
            new FakeCurrentUserContext(Guid.NewGuid(), currentAccountId));

        var result = await controller.GetExportJob(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetExportJob_ReturnsCompletedStatusAndLinkedFileAsset()
    {
        var currentAccountId = Guid.NewGuid();
        var exportJobId = Guid.NewGuid();
        var fileAssetId = Guid.NewGuid();

        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(exportJobId, "Completed", fileAssetId)),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(
                exportJobs:
                [
                    new ExportJob
                    {
                        Id = exportJobId,
                        AccountId = currentAccountId,
                        PlanId = Guid.NewGuid(),
                        ExportType = "Pdf",
                        Status = "Completed",
                        FileAssetId = fileAssetId,
                        IsDeleted = false,
                    },
                ]),
            new FakeCurrentUserContext(Guid.NewGuid(), currentAccountId));

        var result = await controller.GetExportJob(exportJobId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<ExportJobResponse>(ok.Value);
        Assert.Equal("Completed", payload.Status);
        Assert.Equal(fileAssetId, payload.FileAssetId);
    }

    [Fact]
    public async Task ValidCompletedExport_DownloadsFileSuccessfully()
    {
        await using var stream = new MemoryStream([1, 2, 3]);
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.Success(stream, "plan-lccap.pdf", "application/pdf")),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("plan-lccap.pdf", file.FileDownloadName);
    }

    [Fact]
    public async Task CrossTenantExportDownload_Returns404()
    {
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task QueuedOrRunningExportDownload_Returns409()
    {
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.Conflict()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task CompletedExportWithMissingFileAsset_Returns409()
    {
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", null)),
            new FakeDownloadExportQuery(DownloadExportResult.Conflict()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ConflictResult>(result);
    }

    [Fact]
    public async Task DeletedFileAsset_Returns404()
    {
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.NotFound()),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DownloadResponse_DoesNotExposeStoredPath()
    {
        await using var stream = new MemoryStream([1]);
        var controller = new ExportController(
            new FakeCreateExportJobCommand(CreateExportJobResult.Created(Guid.NewGuid(), "Completed", Guid.NewGuid())),
            new FakeDownloadExportQuery(DownloadExportResult.Success(stream, "safe-name.pdf", "application/pdf")),
            new FakeDbContext(),
            new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()));

        var result = await controller.DownloadExport(Guid.NewGuid(), CancellationToken.None);

        var file = Assert.IsType<FileStreamResult>(result);
        Assert.DoesNotContain("uploads/", file.FileDownloadName ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\", file.FileDownloadName ?? string.Empty, StringComparison.Ordinal);
    }

    private sealed class FakeDownloadExportQuery : DownloadExportQuery
    {
        private readonly DownloadExportResult _result;

        public FakeDownloadExportQuery(DownloadExportResult result)
            : base(new FakeDbContext(), new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()), new FakeFileStorageService())
        {
            _result = result;
        }

        public override Task<DownloadExportResult> ExecuteAsync(Guid exportJobId, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeCreateExportJobCommand : CreateExportJobCommand
    {
        private readonly CreateExportJobResult _result;

        public FakeCreateExportJobCommand(CreateExportJobResult result)
            : base(new FakeDbContext(), new FakeCurrentUserContext(Guid.NewGuid(), Guid.NewGuid()), new FakeFileStorageService())
        {
            _result = result;
        }

        public override Task<CreateExportJobResult> ExecuteAsync(CreateExportJobRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public FakeCurrentUserContext(Guid? userId, Guid? accountId)
        {
            UserId = userId;
            AccountId = accountId;
        }

        public Guid? UserId { get; }
        public Guid? AccountId { get; }
        public bool IsAuthenticated => true;
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<StoredFileResult> SaveAsync(
            Stream stream,
            string originalFileName,
            string contentType,
            Guid accountId,
            CancellationToken cancellationToken)
            => Task.FromResult(new StoredFileResult("fake.pdf", "uploads/fake.pdf", "application/pdf", ".pdf", 1, "00", "Local"));

        public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream([1]));

        public Task DeleteAsync(string storedPath, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeDbContext : ILccapDbContext
    {
        public FakeDbContext(IEnumerable<ExportJob>? exportJobs = null)
        {
            ExportJobs = new TestAsyncDbSet<ExportJob>(exportJobs ?? []);
        }

        public DbSet<Plan> Plans => new TestAsyncDbSet<Plan>([]);
        public DbSet<PlanSection> PlanSections => new TestAsyncDbSet<PlanSection>([]);
        public DbSet<ActionItem> ActionItems => new TestAsyncDbSet<ActionItem>([]);
        public DbSet<FileAsset> FileAssets => new TestAsyncDbSet<FileAsset>([]);
        public DbSet<Document> Documents => new TestAsyncDbSet<Document>([]);
        public DbSet<AuditLog> AuditLogs => new TestAsyncDbSet<AuditLog>([]);
        public DbSet<ExportJob> ExportJobs { get; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class TestAsyncDbSet<T> : DbSet<T>, IQueryable<T>
        where T : class
    {
        private readonly IQueryable<T> _queryable;

        public TestAsyncDbSet(IEnumerable<T> data)
        {
            _queryable = data.AsQueryable();
        }

        public override EntityEntry<T> Add(T entity) => throw new NotSupportedException();
        public override Microsoft.EntityFrameworkCore.Metadata.IEntityType EntityType => throw new NotSupportedException();
        public Type ElementType => _queryable.ElementType;
        public Expression Expression => _queryable.Expression;
        public IQueryProvider Provider => _queryable.Provider;
        public IEnumerator<T> GetEnumerator() => _queryable.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _queryable.GetEnumerator();
    }
}
