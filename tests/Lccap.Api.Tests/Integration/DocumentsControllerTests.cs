using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Models;
using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using Lccap.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Api.Tests.Integration;

public sealed class DocumentsControllerTests
{
    [Fact]
    public async Task ValidUploadSucceeds()
    {
        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery(Array.Empty<DocumentListItem>()));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("a.pdf", "application/pdf", 100) },
            CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task MissingFileReturns400()
    {
        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.ValidationError("File is required.")),
            new FakeGetDocumentsByPlanQuery(Array.Empty<DocumentListItem>()));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = null },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CrossTenantPlanUploadReturns404()
    {
        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.PlanNotFound("Plan not found.")),
            new FakeGetDocumentsByPlanQuery(Array.Empty<DocumentListItem>()));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("a.pdf", "application/pdf", 100) },
            CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDocumentsReturnsOnlySameAccountAndPlan()
    {
        var planId = Guid.NewGuid();
        var expected = new[]
        {
            new DocumentListItem(Guid.NewGuid(), planId, Guid.NewGuid(), "Reference", "Doc A", null, DateTimeOffset.UtcNow),
        };

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery(expected));

        var result = await controller.GetByPlan(planId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<DocumentListItem>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(planId, payload[0].PlanId);
    }

    [Fact]
    public async Task InvalidExtensionReturns400()
    {
        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.ValidationError("File type is not allowed.")),
            new FakeGetDocumentsByPlanQuery(Array.Empty<DocumentListItem>()));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("bad.exe", "application/octet-stream", 100) },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    private sealed class FakeUploadDocumentCommand : UploadDocumentCommand
    {
        private readonly UploadDocumentResult _result;

        public FakeUploadDocumentCommand(UploadDocumentResult result)
            : base(new FakeDbContext(), new FakeCurrentUserContext(), new FakeFileStorageService())
        {
            _result = result;
        }

        public override Task<UploadDocumentResult> ExecuteAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeGetDocumentsByPlanQuery : GetDocumentsByPlanQuery
    {
        private readonly IReadOnlyList<DocumentListItem> _items;

        public FakeGetDocumentsByPlanQuery(IReadOnlyList<DocumentListItem> items)
            : base(new FakeDbContext(), new FakeCurrentUserContext())
        {
            _items = items;
        }

        public override Task<IReadOnlyList<DocumentListItem>> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items);
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId => Guid.NewGuid();

        public Guid? AccountId => Guid.NewGuid();

        public bool IsAuthenticated => true;
    }

    private sealed class FakeDbContext : ILccapDbContext
    {
        public DbSet<Plan> Plans => null!;

        public DbSet<FileAsset> FileAssets => null!;

        public DbSet<Document> Documents => null!;

        public DbSet<ActionItem> ActionItems => null!;

        public DbSet<PlanSection> PlanSections => null!;

        public DbSet<ExportJob> ExportJobs => null!;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        private static readonly StoredFileResult Result = new(
            StoredFileName: "deterministic.pdf",
            StoredPath: "uploads/00000000-0000-0000-0000-000000000000/2026/04/deterministic.pdf",
            ContentType: "application/pdf",
            FileExtension: ".pdf",
            FileSizeBytes: 1,
            Sha256Hash: "00",
            StorageProvider: "Local");

        public Task<StoredFileResult> SaveAsync(
            Stream stream,
            string originalFileName,
            string contentType,
            Guid accountId,
            CancellationToken cancellationToken) => Task.FromResult(Result);

        public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken) =>
            Task.FromResult<Stream>(new MemoryStream([1]));

        public Task DeleteAsync(string storedPath, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeFormFile : Microsoft.AspNetCore.Http.IFormFile
    {
        public FakeFormFile(string fileName, string contentType, long length)
        {
            FileName = fileName;
            ContentType = contentType;
            Length = length;
        }

        public string ContentType { get; }
        public string ContentDisposition => string.Empty;
        public IHeaderDictionary Headers => new HeaderDictionary();
        public long Length { get; }
        public string Name => "file";
        public string FileName { get; }
        public void CopyTo(Stream target) => target.Write(new byte[] { 1 }, 0, 1);
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) => target.WriteAsync(new byte[] { 1 }, cancellationToken).AsTask();
        public Stream OpenReadStream() => new MemoryStream(new byte[] { 1 });
    }
}
