using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Models;
using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Lccap.Api.Tests.Integration;

public sealed class DocumentsControllerTests
{
    [Fact]
    public async Task ValidUploadSucceeds()
    {
        var controller = CreateController(
            uploadResult: UploadDocumentResult.Created(Guid.NewGuid()));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("a.pdf", "application/pdf", 100) },
            CancellationToken.None);

        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task MissingFileReturns400()
    {
        var controller = CreateController(
            uploadResult: UploadDocumentResult.ValidationError("File is required."));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = null },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CrossTenantPlanUploadReturns404()
    {
        var controller = CreateController(
            uploadResult: UploadDocumentResult.PlanNotFound("Plan not found."));

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
            ListItem(
                id: Guid.NewGuid(),
                planId: planId,
                title: "Doc A"),
        };

        var controller = CreateController(getDocumentsResult: expected);

        var result = await controller.GetByPlan(planId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<DocumentListItem>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(planId, payload[0].PlanId);
    }

    [Fact]
    public async Task GetDocumentsResponseJsonExcludesStoredPath()
    {
        var planId = Guid.NewGuid();
        var items = new[]
        {
            ListItem(Guid.NewGuid(), planId, "Doc"),
        };

        var controller = CreateController(getDocumentsResult: items);

        var result = await controller.GetByPlan(planId, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("storedPath", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stored_path", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidExtensionReturns400()
    {
        var controller = CreateController(
            uploadResult: UploadDocumentResult.ValidationError("File type is not allowed."));

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("bad.exe", "application/octet-stream", 100) },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_document_metadata_updates_allowed_fields_and_returns_updated_document()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new UpdateDocumentMetadataCommand(db, ctx),
            new FakeArchiveDocumentCommand(ArchiveDocumentResult.CreateNotFound()),
            ctx);

        var body = new UpdateDocumentMetadataApiRequest
        {
            Category = "Map",
            Title = "  Revised title  ",
            Description = "  ",
            DocumentDate = new DateOnly(2024, 6, 1),
            SourceAgency = " LGU ",
            Tags = ["alpha", "Beta", "alpha"],
        };

        var result = await controller.UpdateMetadata(doc.Id, body, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<DocumentListItem>(ok.Value);
        Assert.Equal("Map", dto.Category);
        Assert.Equal("Revised title", dto.Title);
        Assert.Null(dto.Description);
        Assert.Equal(new DateOnly(2024, 6, 1), dto.DocumentDate);
        Assert.Equal("LGU", dto.SourceAgency);
        Assert.Equal(new[] { "alpha", "Beta" }, dto.Tags);

        var reloaded = await db.Documents.SingleAsync(d => d.Id == doc.Id);
        Assert.Equal("Map", reloaded.Category);
        Assert.Equal("Revised title", reloaded.Title);
    }

    [Fact]
    public async Task Update_document_metadata_rejects_invalid_category()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new UpdateDocumentMetadataCommand(db, ctx),
            new FakeArchiveDocumentCommand(ArchiveDocumentResult.CreateNotFound()),
            ctx);

        var body = new UpdateDocumentMetadataApiRequest
        {
            Category = "NotARealCategory",
            Title = "x",
        };

        var result = await controller.UpdateMetadata(doc.Id, body, CancellationToken.None);
        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Update_document_metadata_rejects_cross_tenant_document()
    {
        await using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (_, _, doc) = await SeedDocumentGraph(db, ownerAccount);
        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new UpdateDocumentMetadataCommand(db, ctx),
            new FakeArchiveDocumentCommand(ArchiveDocumentResult.CreateNotFound()),
            ctx);

        var body = new UpdateDocumentMetadataApiRequest { Category = "Reference", Title = "x" };
        var result = await controller.UpdateMetadata(doc.Id, body, CancellationToken.None);
        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_document_metadata_rejects_deleted_document()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);
        doc.IsDeleted = true;
        _ = await db.SaveChangesAsync();

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new UpdateDocumentMetadataCommand(db, ctx),
            new FakeArchiveDocumentCommand(ArchiveDocumentResult.CreateNotFound()),
            ctx);

        var body = new UpdateDocumentMetadataApiRequest { Category = "Reference", Title = "x" };
        var result = await controller.UpdateMetadata(doc.Id, body, CancellationToken.None);
        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Archive_document_sets_soft_delete_fields()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult.CreateNotFound()),
            new ArchiveDocumentCommand(db, ctx),
            ctx);

        var result = await controller.Archive(doc.Id, CancellationToken.None);
        _ = Assert.IsType<NoContentResult>(result);

        var reloaded = await db.Documents.SingleAsync(d => d.Id == doc.Id);
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.DeletedAtUtc);
        Assert.Equal(userId, reloaded.DeletedByUserId);
    }

    [Fact]
    public async Task Archive_document_does_not_delete_or_archive_file_asset()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, file, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult.CreateNotFound()),
            new ArchiveDocumentCommand(db, ctx),
            ctx);

        _ = await controller.Archive(doc.Id, CancellationToken.None);

        var reloadedFile = await db.FileAssets.SingleAsync(f => f.Id == file.Id);
        Assert.False(reloadedFile.IsDeleted);
    }

    [Fact]
    public async Task Archive_document_hides_document_from_plan_documents_list()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (plan, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult.CreateNotFound()),
            new ArchiveDocumentCommand(db, ctx),
            ctx);

        _ = await controller.Archive(doc.Id, CancellationToken.None);

        var query = new GetDocumentsByPlanQuery(db, ctx);
        var list = await query.ExecuteAsync(plan.Id, CancellationToken.None);
        Assert.Empty(list);
    }

    [Fact]
    public async Task Archive_document_rejects_cross_tenant_document()
    {
        await using var db = CreateDbContext();
        var ownerAccount = Guid.NewGuid();
        var otherAccount = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (_, _, doc) = await SeedDocumentGraph(db, ownerAccount);
        var ctx = new TestCurrentUserContext(otherAccount, userId, true, WorkspaceRoles.Admin);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult.CreateNotFound()),
            new ArchiveDocumentCommand(db, ctx),
            ctx);

        var result = await controller.Archive(doc.Id, CancellationToken.None);
        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_document_metadata_writes_audit_log_with_old_and_new_values()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new UpdateDocumentMetadataCommand(db, ctx),
            new FakeArchiveDocumentCommand(ArchiveDocumentResult.CreateNotFound()),
            ctx);

        var body = new UpdateDocumentMetadataApiRequest
        {
            Category = "Map",
            Title = "Next",
            Tags = ["t1"],
        };

        _ = await controller.UpdateMetadata(doc.Id, body, CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(
            a => a.EntityId == doc.Id && a.Action == "DocumentMetadataUpdated");

        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("Document", log.EntityName);
        Assert.NotNull(log.OldValuesJson);
        Assert.NotNull(log.NewValuesJson);

        var oldTitle = log.OldValuesJson!.RootElement.GetProperty("title").GetString();
        var newTitle = log.NewValuesJson!.RootElement.GetProperty("title").GetString();

        Assert.Equal("Seed Doc", oldTitle);
        Assert.Equal("Next", newTitle);
    }

    [Fact]
    public async Task Archive_document_writes_audit_log()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ctx = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);
        var (_, _, doc) = await SeedDocumentGraph(db, accountId);

        var controller = new DocumentsController(
            new FakeUploadDocumentCommand(UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery([]),
            new FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult.CreateNotFound()),
            new ArchiveDocumentCommand(db, ctx),
            ctx);

        _ = await controller.Archive(doc.Id, CancellationToken.None);

        var log = await db.AuditLogs.SingleAsync(
            a => a.EntityId == doc.Id && a.Action == "DocumentArchived");

        Assert.Equal(accountId, log.AccountId);
        Assert.Equal(userId, log.UserId);
        Assert.Equal("Document", log.EntityName);
        Assert.True(log.NewValuesJson!.RootElement.GetProperty("isDeleted").GetBoolean());

        var meta = log.MetadataJson.RootElement;
        Assert.Equal("SoftDelete", meta.GetProperty("archiveType").GetString());
    }

    [Fact]
    public async Task Viewer_cannot_upload_document()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var controller = CreateController(role: WorkspaceRoles.Viewer);

        var result = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("a.pdf", "application/pdf", 100) },
            CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_update_document_metadata()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var controller = CreateController(role: WorkspaceRoles.Viewer);

        var result = await controller.UpdateMetadata(Guid.NewGuid(), new UpdateDocumentMetadataApiRequest { Category = "Reference", Title = "x" }, CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_archive_document()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Viewer);
        var controller = CreateController(role: WorkspaceRoles.Viewer);

        var result = await controller.Archive(Guid.NewGuid(), CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Planner_can_upload_and_update_document()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Planner);
        var controller = CreateController(
            role: WorkspaceRoles.Planner,
            uploadResult: UploadDocumentResult.Created(Guid.NewGuid()),
            updateResult: UpdateDocumentMetadataResult.CreateSuccess(ListItem(Guid.NewGuid(), Guid.NewGuid(), "x")));

        var uploadResult = await controller.Upload(
            new UploadDocumentFormRequest { PlanId = Guid.NewGuid(), Category = "Reference", Title = "Doc", File = new FakeFormFile("a.pdf", "application/pdf", 100) },
            CancellationToken.None);
        _ = Assert.IsType<CreatedResult>(uploadResult);

        var updateResult = await controller.UpdateMetadata(Guid.NewGuid(), new UpdateDocumentMetadataApiRequest { Category = "Reference", Title = "x" }, CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(updateResult);
    }

    [Fact]
    public async Task Admin_can_archive_document()
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, WorkspaceRoles.Admin);
        var controller = CreateController(
            role: WorkspaceRoles.Admin,
            archiveResult: ArchiveDocumentResult.CreateSuccess(Guid.NewGuid()));

        var result = await controller.Archive(Guid.NewGuid(), CancellationToken.None);

        _ = Assert.IsType<NoContentResult>(result);
    }

    private static DocumentsController CreateController(
        UploadDocumentResult? uploadResult = null,
        IReadOnlyList<DocumentListItem>? getDocumentsResult = null,
        UpdateDocumentMetadataResult? updateResult = null,
        ArchiveDocumentResult? archiveResult = null,
        string role = WorkspaceRoles.Admin)
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, role);
        return new DocumentsController(
            new FakeUploadDocumentCommand(uploadResult ?? UploadDocumentResult.Created(Guid.NewGuid())),
            new FakeGetDocumentsByPlanQuery(getDocumentsResult ?? []),
            new FakeUpdateDocumentMetadataCommand(updateResult ?? UpdateDocumentMetadataResult.CreateNotFound()),
            new FakeArchiveDocumentCommand(archiveResult ?? ArchiveDocumentResult.CreateNotFound()),
            ctx);
    }

    private static DocumentListItem ListItem(Guid id, Guid planId, string title) =>
        new(
            id,
            planId,
            Guid.NewGuid(),
            "Reference",
            title,
            null,
            null,
            null,
            Array.Empty<string>(),
            "a.pdf",
            "application/pdf",
            99L,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private static async Task<(Plan plan, FileAsset file, Document doc)> SeedDocumentGraph(LccapDbContext db, Guid accountId)
    {
        var plan = new Plan
        {
            AccountId = accountId,
            Title = "Doc Plan",
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

        var fileId = Guid.NewGuid();
        var file = new FileAsset
        {
            Id = fileId,
            AccountId = accountId,
            OwnerType = "PlanDocument",
            OwnerId = plan.Id,
            OriginalFileName = "seed.pdf",
            StoredFileName = "seed.pdf",
            StoredPath = "uploads/secret/seed.pdf",
            ContentType = "application/pdf",
            FileExtension = ".pdf",
            FileSizeBytes = 10,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            MetadataJson = JsonDocument.Parse("{}"),
        };

        var doc = new Document
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            FileAssetId = fileId,
            Category = "Reference",
            Title = "Seed Doc",
            Description = "Desc",
            TagsJson = JsonDocument.Parse("[\"z\"]"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = db.FileAssets.Add(file);
        _ = db.Documents.Add(doc);
        _ = await db.SaveChangesAsync();
        return (plan, file, doc);
    }

    private static LccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase($"documents-tests-{Guid.NewGuid()}")
            .Options;

        return new DocumentsModuleTestDbContext(options);
    }

    private sealed class DocumentsModuleTestDbContext : LccapDbContext
    {
        public DocumentsModuleTestDbContext(DbContextOptions<LccapDbContext> options)
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

    private sealed class FakeUploadDocumentCommand : UploadDocumentCommand
    {
        private readonly UploadDocumentResult _result;

        public FakeUploadDocumentCommand(UploadDocumentResult result)
            : base(new DocumentsFakeDbContext(), new FakeCurrentUserContext(), new FakeFileStorageService())
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
            : base(new DocumentsFakeDbContext(), new FakeCurrentUserContext())
        {
            _items = items;
        }

        public override Task<IReadOnlyList<DocumentListItem>> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult(_items);
    }

    private sealed class FakeUpdateDocumentMetadataCommand : UpdateDocumentMetadataCommand
    {
        private readonly UpdateDocumentMetadataResult _result;

        public FakeUpdateDocumentMetadataCommand(UpdateDocumentMetadataResult result)
            : base(new DocumentsFakeDbContext(), new FakeCurrentUserContext())
        {
            _result = result;
        }

        public override Task<UpdateDocumentMetadataResult> ExecuteAsync(
            Guid documentId,
            UpdateDocumentMetadataRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeArchiveDocumentCommand : ArchiveDocumentCommand
    {
        private readonly ArchiveDocumentResult _result;

        public FakeArchiveDocumentCommand(ArchiveDocumentResult result)
            : base(new DocumentsFakeDbContext(), new FakeCurrentUserContext())
        {
            _result = result;
        }

        public override Task<ArchiveDocumentResult> ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeCurrentUserContext : ICurrentUserContext
    {
        public Guid? UserId => Guid.NewGuid();

        public Guid? AccountId => Guid.NewGuid();

        public string? Role => WorkspaceRoles.Admin;

        public bool IsAuthenticated => true;
    }

    private sealed class DocumentsFakeDbContext : ILccapDbContext
    {
        public DbSet<Plan> Plans => null!;

        public DbSet<FileAsset> FileAssets => null!;

        public DbSet<Document> Documents => null!;

        public DbSet<ActionItem> ActionItems => null!;

        public DbSet<MonitoringIndicator> MonitoringIndicators => null!;

        public DbSet<PlanSection> PlanSections => null!;

        public DbSet<ExportJob> ExportJobs => null!;

        public DbSet<AuditLog> AuditLogs => null!;

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

    private sealed class FakeFormFile : IFormFile
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
        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            target.WriteAsync(new byte[] { 1 }, cancellationToken).AsTask();
        public Stream OpenReadStream() => new MemoryStream(new byte[] { 1 });
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
