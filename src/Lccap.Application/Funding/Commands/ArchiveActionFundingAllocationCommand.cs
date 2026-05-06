using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Commands;

public sealed class ArchiveActionFundingAllocationCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveActionFundingAllocationCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<ArchiveActionFundingAllocationResult> ExecuteAsync(
        Guid allocationId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return ArchiveActionFundingAllocationResult.CreateForbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var entity = await _dbContext.ActionFundingAllocations.FirstOrDefaultAsync(
            a => a.Id == allocationId && a.AccountId == accountId && !a.IsDeleted,
            cancellationToken);

        if (entity is null)
        {
            return ArchiveActionFundingAllocationResult.CreateNotFound();
        }

        var oldValues = JsonSerializer.SerializeToDocument(
            new
            {
                entity.Id,
                entity.PlanId,
                entity.ActionItemId,
                entity.IsDeleted,
            },
            AuditJsonOptions);

        var now = DateTimeOffset.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAtUtc = now;
        entity.DeletedByUserId = userId;
        entity.UpdatedAtUtc = now;
        entity.UpdatedByUserId = userId;

        var newValues = JsonSerializer.SerializeToDocument(
            new
            {
                isDeleted = true,
                deletedAtUtc = now.ToString("O"),
                deletedByUserId = userId,
            },
            AuditJsonOptions);

        var metadata = JsonSerializer.SerializeToDocument(
            new { entity.PlanId, entity.ActionItemId },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "ActionFundingAllocation",
            EntityId = entity.Id,
            Action = "ActionFundingAllocationArchived",
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        _ = _dbContext.AuditLogs.Add(audit);

        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return ArchiveActionFundingAllocationResult.CreateSuccess();
    }
}

public sealed record ArchiveActionFundingAllocationResult(bool Success, bool NotFound, bool ForbiddenAccount)
{
    public static ArchiveActionFundingAllocationResult CreateSuccess() => new(true, false, false);

    public static ArchiveActionFundingAllocationResult CreateNotFound() => new(false, true, false);

    public static ArchiveActionFundingAllocationResult CreateForbidden() => new(false, false, true);
}
