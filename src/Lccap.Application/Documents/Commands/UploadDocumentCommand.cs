using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Commands;

public class UploadDocumentCommand
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".xlsx",
        ".png",
        ".jpg",
        ".jpeg",
    };

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clup",
        "Cdp",
        "Drrm",
        "HazardStudy",
        "ClimateData",
        "Map",
        "Reference",
        "Other",
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFileStorageService _fileStorageService;

    public UploadDocumentCommand(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _fileStorageService = fileStorageService;
    }

    public virtual async Task<UploadDocumentResult> ExecuteAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue)
        {
            return UploadDocumentResult.ValidationError("Authenticated account is required.");
        }

        var accountId = _currentUserContext.AccountId.Value;

        if (request.File is null)
        {
            return UploadDocumentResult.ValidationError("File is required.");
        }

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            return UploadDocumentResult.ValidationError("File name is required.");
        }

        if (request.FileSizeBytes <= 0)
        {
            return UploadDocumentResult.ValidationError("File size must be greater than zero.");
        }

        var extension = Path.GetExtension(request.FileName);
        if (string.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension))
        {
            return UploadDocumentResult.ValidationError("File type is not allowed.");
        }

        if (string.IsNullOrWhiteSpace(request.Category) || !AllowedCategories.Contains(request.Category))
        {
            return UploadDocumentResult.ValidationError("Document category is invalid.");
        }

        var planExists = await _dbContext.Plans.AnyAsync(
            p => p.Id == request.PlanId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);

        if (!planExists)
        {
            return UploadDocumentResult.PlanNotFound("Plan not found.");
        }

        var now = DateTime.UtcNow;
        var fileAssetId = Guid.NewGuid();
        var documentId = Guid.NewGuid();
        var storedFile = await _fileStorageService.SaveAsync(
            request.File,
            request.FileName,
            request.ContentType ?? "application/octet-stream",
            accountId,
            cancellationToken);

        var fileAsset = new FileAsset
        {
            Id = fileAssetId,
            AccountId = accountId,
            OwnerType = "PlanDocument",
            OwnerId = request.PlanId,
            OriginalFileName = request.FileName,
            StoredFileName = storedFile.StoredFileName,
            StoredPath = storedFile.StoredPath,
            ContentType = storedFile.ContentType,
            FileExtension = storedFile.FileExtension,
            FileSizeBytes = storedFile.FileSizeBytes,
            StorageProvider = storedFile.StorageProvider,
            UploadedByUserId = _currentUserContext.UserId,
            CreatedAtUtc = now,
            CreatedByUserId = _currentUserContext.UserId,
            IsDeleted = false,
        };

        var document = new Document
        {
            Id = documentId,
            AccountId = accountId,
            PlanId = request.PlanId,
            FileAssetId = fileAssetId,
            Category = request.Category,
            Title = request.Title,
            Description = request.Description,
            UploadedByUserId = _currentUserContext.UserId,
            CreatedAtUtc = now,
            CreatedByUserId = _currentUserContext.UserId,
            IsDeleted = false,
        };

        _ = _dbContext.FileAssets.Add(fileAsset);
        _ = _dbContext.Documents.Add(document);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return UploadDocumentResult.Created(documentId);
    }
}

public sealed record UploadDocumentRequest(
    Guid PlanId,
    string Category,
    string? Title,
    string? Description,
    Stream? File,
    string FileName,
    string? ContentType,
    long FileSizeBytes);

public sealed record UploadDocumentResult(bool Success, bool NotFound, Guid? DocumentId, string? Error)
{
    public static UploadDocumentResult Created(Guid documentId) => new(true, false, documentId, null);

    public static UploadDocumentResult ValidationError(string error) => new(false, false, null, error);

    public static UploadDocumentResult PlanNotFound(string error) => new(false, true, null, error);
}
