using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Queries;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Commands;

public class UpdateDocumentMetadataCommand
{
    private static readonly string[] CanonicalCategories =
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

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateDocumentMetadataCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<UpdateDocumentMetadataResult> ExecuteAsync(
        Guid documentId,
        UpdateDocumentMetadataRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue)
        {
            return UpdateDocumentMetadataResult.CreateUnauthorizedAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;

        if (!TryNormalizeCategory(request.Category, out var normalizedCategory, out var categoryError))
        {
            return UpdateDocumentMetadataResult.CreateValidationError(categoryError!);
        }

        var titleResult = NormalizeTitle(request.Title, out var normalizedTitle, out var titleError);
        if (!titleResult)
        {
            return UpdateDocumentMetadataResult.CreateValidationError(titleError!);
        }

        var description = NormalizeOptionalText(request.Description);
        var sourceResult = NormalizeSourceAgency(request.SourceAgency, out var sourceAgency, out var sourceError);
        if (!sourceResult)
        {
            return UpdateDocumentMetadataResult.CreateValidationError(sourceError!);
        }

        if (!TryNormalizeTags(request.Tags, out var tags, out var tagsError))
        {
            return UpdateDocumentMetadataResult.CreateValidationError(tagsError!);
        }

        var document = await _dbContext.Documents
            .Include(d => d.Plan)
            .FirstOrDefaultAsync(
                d => d.Id == documentId && d.AccountId == accountId && !d.IsDeleted,
                cancellationToken);

        if (document is null)
        {
            return UpdateDocumentMetadataResult.CreateNotFound();
        }

        if (document.Plan.IsDeleted || document.Plan.AccountId != accountId)
        {
            return UpdateDocumentMetadataResult.CreateNotFound();
        }

        var oldSnapshot = BuildMetadataSnapshotJson(
            document.Title,
            document.Category,
            document.Description,
            document.DocumentDate,
            document.SourceAgency,
            DocumentTagParsing.ParseTags(document.TagsJson));

        document.UpdateMetadata(
            normalizedCategory,
            normalizedTitle,
            description,
            request.DocumentDate,
            sourceAgency,
            JsonDocument.Parse(JsonSerializer.Serialize(tags)),
            DateTimeOffset.UtcNow,
            _currentUserContext.UserId);

        var newSnapshot = BuildMetadataSnapshotJson(
            document.Title,
            document.Category,
            document.Description,
            document.DocumentDate,
            document.SourceAgency,
            tags);

        var metadata = JsonSerializer.SerializeToDocument(
            new { planId = document.PlanId, fileAssetId = document.FileAssetId },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = _currentUserContext.UserId,
            EntityName = "Document",
            EntityId = document.Id,
            Action = "DocumentMetadataUpdated",
            OldValuesJson = oldSnapshot,
            NewValuesJson = newSnapshot,
            MetadataJson = metadata,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = _dbContext.AuditLogs.Add(audit);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        var file = await _dbContext.FileAssets.AsNoTracking()
            .FirstAsync(f => f.Id == document.FileAssetId, cancellationToken);

        var listItem = new DocumentListItem(
            document.Id,
            document.PlanId,
            document.FileAssetId,
            document.Category,
            document.Title,
            document.Description,
            document.DocumentDate,
            document.SourceAgency,
            tags,
            file.OriginalFileName,
            file.ContentType,
            file.FileSizeBytes,
            file.CreatedAtUtc,
            document.CreatedAtUtc);

        return UpdateDocumentMetadataResult.CreateSuccess(listItem);
    }

    private static JsonDocument BuildMetadataSnapshotJson(
        string? title,
        string category,
        string? description,
        DateOnly? documentDate,
        string? sourceAgency,
        IReadOnlyList<string> tags)
    {
        return JsonSerializer.SerializeToDocument(
            new
            {
                title,
                category,
                description,
                documentDate,
                sourceAgency,
                tags,
            },
            AuditJsonOptions);
    }

    private static bool TryNormalizeCategory(string? input, out string normalized, out string? error)
    {
        normalized = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Document category is required.";
            return false;
        }

        var trimmed = input.Trim();
        foreach (var c in CanonicalCategories)
        {
            if (string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                normalized = c;
                return true;
            }
        }

        error = "Document category is invalid.";
        return false;
    }

    private static bool NormalizeTitle(string? title, out string? normalized, out string? error)
    {
        error = null;
        if (title is null)
        {
            normalized = null;
            return true;
        }

        var t = title.Trim();
        if (t.Length == 0)
        {
            normalized = null;
            return true;
        }

        if (t.Length > 250)
        {
            normalized = null;
            error = "Title must be 250 characters or fewer.";
            return false;
        }

        normalized = t;
        return true;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var t = value.Trim();
        return t.Length == 0 ? null : t;
    }

    private static bool NormalizeSourceAgency(string? value, out string? normalized, out string? error)
    {
        error = null;
        if (value is null)
        {
            normalized = null;
            return true;
        }

        var t = value.Trim();
        if (t.Length == 0)
        {
            normalized = null;
            return true;
        }

        if (t.Length > 200)
        {
            normalized = null;
            error = "Source agency must be 200 characters or fewer.";
            return false;
        }

        normalized = t;
        return true;
    }

    private static bool TryNormalizeTags(IReadOnlyList<string>? tags, out List<string> normalized, out string? error)
    {
        normalized = new List<string>();
        error = null;

        if (tags is null || tags.Count == 0)
        {
            return true;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in tags)
        {
            var t = raw.Trim();
            if (t.Length == 0)
            {
                continue;
            }

            if (t.Length > 50)
            {
                error = "Each tag must be 50 characters or fewer.";
                return false;
            }

            if (!seen.Add(t))
            {
                continue;
            }

            normalized.Add(t);
            if (normalized.Count > 20)
            {
                error = "At most 20 tags are allowed.";
                return false;
            }
        }

        return true;
    }
}

public sealed record UpdateDocumentMetadataRequest(
    string Category,
    string? Title,
    string? Description,
    DateOnly? DocumentDate,
    string? SourceAgency,
    IReadOnlyList<string>? Tags);

public sealed record UpdateDocumentMetadataResult(
    bool Success,
    bool NotFound,
    bool UnauthorizedAccount,
    DocumentListItem? Item,
    string? Error)
{
    public static UpdateDocumentMetadataResult CreateSuccess(DocumentListItem item) =>
        new(true, false, false, item, null);

    public static UpdateDocumentMetadataResult CreateNotFound() =>
        new(false, true, false, null, null);

    public static UpdateDocumentMetadataResult CreateUnauthorizedAccount() =>
        new(false, false, true, null, null);

    public static UpdateDocumentMetadataResult CreateValidationError(string error) =>
        new(false, false, false, null, error);
}
