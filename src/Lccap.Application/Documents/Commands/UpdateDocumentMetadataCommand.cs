using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Concurrency;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Queries;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Commands;

public class UpdateDocumentMetadataCommand
{
    private static readonly HashSet<string> AllowedEvidenceStatuses = new(StringComparer.Ordinal)
    {
        "Draft",
        "Internal",
        "Official",
        "Public"
    };

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

        var evidenceStatus = NormalizeEvidenceStatus(request.EvidenceStatus, out var evidenceStatusError);
        if (evidenceStatus is null)
        {
            return UpdateDocumentMetadataResult.CreateValidationError(evidenceStatusError!);
        }

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

        if (request.PlanSectionId.HasValue)
        {
            var sectionExists = await _dbContext.PlanSections.AnyAsync(
                s =>
                    s.Id == request.PlanSectionId.Value
                    && s.PlanId == document.PlanId
                    && s.AccountId == accountId
                    && !s.IsDeleted,
                cancellationToken);

            if (!sectionExists)
            {
                return UpdateDocumentMetadataResult.CreateValidationError("Linked section is invalid.");
            }
        }

        if (request.ActionItemId.HasValue)
        {
            var actionExists = await _dbContext.ActionItems.AnyAsync(
                a =>
                    a.Id == request.ActionItemId.Value
                    && a.PlanId == document.PlanId
                    && a.AccountId == accountId
                    && !a.IsDeleted,
                cancellationToken);

            if (!actionExists)
            {
                return UpdateDocumentMetadataResult.CreateValidationError("Linked action item is invalid.");
            }
        }

        var oldSnapshot = BuildMetadataSnapshotJson(
            document.Title,
            document.Category,
            document.Description,
            document.DocumentDate,
            document.SourceAgency,
            document.PlanSectionId,
            document.ActionItemId,
            document.EvidenceStatus,
            DocumentTagParsing.ParseTags(document.TagsJson));

        document.UpdateMetadataWithEvidenceLinks(
            normalizedCategory,
            normalizedTitle,
            description,
            request.DocumentDate,
            sourceAgency,
            JsonDocument.Parse(JsonSerializer.Serialize(tags)),
            request.PlanSectionId,
            request.ActionItemId,
            evidenceStatus,
            DateTimeOffset.UtcNow,
            _currentUserContext.UserId);

        document.RotateRowVersion();

        var newSnapshot = BuildMetadataSnapshotJson(
            document.Title,
            document.Category,
            document.Description,
            document.DocumentDate,
            document.SourceAgency,
            document.PlanSectionId,
            document.ActionItemId,
            document.EvidenceStatus,
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
            RowVersion = RowVersionHelper.NewRowVersion(),
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
            document.PlanSectionId,
            document.ActionItemId,
            document.EvidenceStatus,
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
        Guid? planSectionId,
        Guid? actionItemId,
        string evidenceStatus,
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
                planSectionId,
                actionItemId,
                evidenceStatus,
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

    private static string? NormalizeEvidenceStatus(string? input, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return "Internal";
        }

        var trimmed = input.Trim();
        foreach (var allowed in AllowedEvidenceStatuses)
        {
            if (string.Equals(allowed, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return allowed;
            }
        }

        error = "Evidence status is invalid.";
        return null;
    }
}

public sealed record UpdateDocumentMetadataRequest(
    string Category,
    string? Title,
    string? Description,
    DateOnly? DocumentDate,
    string? SourceAgency,
    Guid? PlanSectionId,
    Guid? ActionItemId,
    string? EvidenceStatus,
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
