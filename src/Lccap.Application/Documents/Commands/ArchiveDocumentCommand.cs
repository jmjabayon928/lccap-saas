using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Queries;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Commands;

public class ArchiveDocumentCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveDocumentCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<ArchiveDocumentResult> ExecuteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return ArchiveDocumentResult.CreateUnauthorizedAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var document = await _dbContext.Documents
            .Include(d => d.Plan)
            .FirstOrDefaultAsync(
                d => d.Id == documentId && d.AccountId == accountId && !d.IsDeleted,
                cancellationToken);

        if (document is null)
        {
            return ArchiveDocumentResult.CreateNotFound();
        }

        if (document.Plan.IsDeleted || document.Plan.AccountId != accountId)
        {
            return ArchiveDocumentResult.CreateNotFound();
        }

        var oldTags = DocumentTagParsing.ParseTags(document.TagsJson);
        var oldValues = JsonSerializer.SerializeToDocument(
            new
            {
                id = document.Id,
                planId = document.PlanId,
                fileAssetId = document.FileAssetId,
                title = document.Title,
                category = document.Category,
                description = document.Description,
                documentDate = document.DocumentDate,
                sourceAgency = document.SourceAgency,
                tags = oldTags,
                isDeleted = document.IsDeleted,
            },
            AuditJsonOptions);

        var now = DateTimeOffset.UtcNow;
        document.Archive(now, userId, userId);

        var newValues = JsonSerializer.SerializeToDocument(
            new
            {
                isDeleted = true,
                deletedAtUtc = now.ToString("O"),
                deletedByUserId = userId,
            },
            AuditJsonOptions);

        var metadata = JsonSerializer.SerializeToDocument(
            new
            {
                planId = document.PlanId,
                fileAssetId = document.FileAssetId,
                archiveType = "SoftDelete",
            },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "Document",
            EntityId = document.Id,
            Action = "DocumentArchived",
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = _dbContext.AuditLogs.Add(audit);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return ArchiveDocumentResult.CreateSuccess(document.Id);
    }
}

public sealed record ArchiveDocumentResult(bool Success, bool NotFound, bool UnauthorizedAccount, Guid? DocumentId)
{
    public static ArchiveDocumentResult CreateSuccess(Guid documentId) =>
        new(true, false, false, documentId);

    public static ArchiveDocumentResult CreateNotFound() =>
        new(false, true, false, null);

    public static ArchiveDocumentResult CreateUnauthorizedAccount() =>
        new(false, false, true, null);
}
