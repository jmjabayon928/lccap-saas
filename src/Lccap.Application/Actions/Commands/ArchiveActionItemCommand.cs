using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Commands;

public class ArchiveActionItemCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveActionItemCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<ArchiveActionItemResult> ExecuteAsync(Guid actionItemId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return ArchiveActionItemResult.CreateUnauthorizedAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var entity = await _dbContext.ActionItems
            .Include(x => x.Plan)
            .FirstOrDefaultAsync(
                x => x.Id == actionItemId && x.AccountId == accountId && !x.IsDeleted,
                cancellationToken);

        if (entity is null)
        {
            return ArchiveActionItemResult.CreateNotFound();
        }

        if (entity.Plan.IsDeleted || entity.Plan.AccountId != accountId)
        {
            return ArchiveActionItemResult.CreateNotFound();
        }

        var oldValues = JsonSerializer.SerializeToDocument(
            new
            {
                id = entity.Id,
                planId = entity.PlanId,
                title = entity.Title,
                description = entity.Description,
                actionType = entity.ActionType,
                sector = entity.Sector,
                responsibleOffice = entity.ResponsibleOffice,
                budgetAmount = entity.BudgetAmount,
                fundingSource = entity.FundingSource,
                timelineStartUtc = entity.TimelineStartUtc,
                timelineEndUtc = entity.TimelineEndUtc,
                kpi = entity.Kpi,
                priorityScore = entity.PriorityScore,
                status = entity.Status,
                isDeleted = entity.IsDeleted,
            },
            AuditJsonOptions);

        var now = DateTimeOffset.UtcNow;
        entity.Archive(now, userId);

        var newValues = JsonSerializer.SerializeToDocument(
            new
            {
                isDeleted = true,
                deletedAtUtc = now.ToString("O"),
                deletedByUserId = userId,
            },
            AuditJsonOptions);

        var metadata = JsonSerializer.SerializeToDocument(
            new { planId = entity.PlanId, archiveType = "SoftDelete" },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "ActionItem",
            EntityId = entity.Id,
            Action = "ActionItemArchived",
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = _dbContext.AuditLogs.Add(audit);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return ArchiveActionItemResult.CreateSuccess();
    }
}

public sealed record ArchiveActionItemResult(bool Success, bool NotFound, bool UnauthorizedAccount)
{
    public static ArchiveActionItemResult CreateSuccess() => new(true, false, false);

    public static ArchiveActionItemResult CreateNotFound() => new(false, true, false);

    public static ArchiveActionItemResult CreateUnauthorizedAccount() => new(false, false, true);
}
