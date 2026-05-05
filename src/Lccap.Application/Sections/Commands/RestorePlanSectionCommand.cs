using System.Text.Json;
using Lccap.Application.Common.Concurrency;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Commands;

public class RestorePlanSectionCommand
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public RestorePlanSectionCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<RestorePlanSectionResult> ExecuteAsync(RestorePlanSectionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return RestorePlanSectionResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == request.PlanId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);

        if (plan == null)
        {
            return RestorePlanSectionResult.Missing();
        }

        if (plan.Status == "Archived")
        {
            return RestorePlanSectionResult.ValidationError("Cannot restore sections of an archived plan.");
        }

        var section = await _dbContext.PlanSections
            .FirstOrDefaultAsync(s => s.PlanId == request.PlanId && s.SectionKey == request.SectionKey && s.AccountId == accountId && !s.IsDeleted, cancellationToken);

        if (section == null)
        {
            return RestorePlanSectionResult.Missing();
        }

        var auditLog = await _dbContext.AuditLogs
            .FirstOrDefaultAsync(a => a.Id == request.AuditLogId 
                && a.AccountId == accountId 
                && a.EntityName == "PlanSection" 
                && a.EntityId == section.Id, cancellationToken);

        if (auditLog == null)
        {
            return RestorePlanSectionResult.ValidationError("Selected revision not found or access denied.");
        }

        if (auditLog.NewValuesJson == null)
        {
            return RestorePlanSectionResult.ValidationError("Selected revision cannot be restored (missing content).");
        }

        string restoredTitle = string.Empty;
        string restoredContent = string.Empty;

        try
        {
            var root = auditLog.NewValuesJson.RootElement;
            if (root.TryGetProperty("title", out var tProp)) restoredTitle = tProp.GetString() ?? string.Empty;
            if (root.TryGetProperty("content", out var cProp)) restoredContent = cProp.GetString() ?? string.Empty;
        }
        catch
        {
            return RestorePlanSectionResult.ValidationError("Selected revision is malformed and cannot be restored.");
        }

        if (string.IsNullOrWhiteSpace(restoredTitle))
        {
            return RestorePlanSectionResult.ValidationError("Selected revision cannot be restored (missing title).");
        }

        var now = DateTimeOffset.UtcNow;

        // Snapshot current state before restore
        var oldValues = new
        {
            sectionId = section.Id,
            planId = section.PlanId,
            sectionKey = section.SectionKey,
            title = section.Title,
            content = section.Content,
            sortOrder = section.SortOrder,
            lastEditedByUserId = section.LastEditedByUserId,
            lastEditedAtUtc = section.LastEditedAtUtc,
            rowVersion = RowVersionHelper.ToBase64(section.RowVersion)
        };

        // Apply restore
        section.UpdateContent(restoredTitle, restoredContent, userId, now);
        section.RotateRowVersion();

        // Snapshot new state after restore
        var newValues = new
        {
            sectionId = section.Id,
            planId = section.PlanId,
            sectionKey = section.SectionKey,
            title = section.Title,
            content = section.Content,
            sortOrder = section.SortOrder,
            lastEditedByUserId = section.LastEditedByUserId,
            lastEditedAtUtc = section.LastEditedAtUtc,
            rowVersion = RowVersionHelper.ToBase64(section.RowVersion)
        };

        var restoreAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "PlanSection",
            EntityId = section.Id,
            Action = "PlanSectionRestored",
            OldValuesJson = JsonDocument.Parse(JsonSerializer.Serialize(oldValues)),
            NewValuesJson = JsonDocument.Parse(JsonSerializer.Serialize(newValues)),
            MetadataJson = JsonDocument.Parse(JsonSerializer.Serialize(new
            {
                planId = request.PlanId,
                sectionKey = request.SectionKey,
                restoredFromAuditLogId = request.AuditLogId,
                restoreReason = request.RestoreReason,
                revisionSource = "AuditLog"
            })),
            CreatedAtUtc = now
        };
        restoreAudit.EnsureRowVersion();
        _ = _dbContext.AuditLogs.Add(restoreAudit);

        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return RestorePlanSectionResult.Ok(section.Id, section.LastEditedByUserId, section.LastEditedAtUtc);
    }
}

public sealed record RestorePlanSectionRequest(
    Guid PlanId,
    string SectionKey,
    Guid AuditLogId,
    string? RestoreReason = null);

public sealed record RestorePlanSectionResult(
    bool Success,
    bool ForbiddenAccess,
    bool NotFound,
    Guid? SectionId,
    Guid? LastEditedByUserId,
    DateTimeOffset? LastEditedAtUtc,
    string? Error)
{
    public static RestorePlanSectionResult Ok(Guid sectionId, Guid? lastEditedByUserId, DateTimeOffset? lastEditedAtUtc) =>
        new(true, false, false, sectionId, lastEditedByUserId, lastEditedAtUtc, null);

    public static RestorePlanSectionResult ValidationError(string error) =>
        new(false, false, false, null, null, null, error);

    public static RestorePlanSectionResult Missing() =>
        new(false, false, true, null, null, null, null);

    public static RestorePlanSectionResult Forbidden() =>
        new(false, true, false, null, null, null, null);
}
