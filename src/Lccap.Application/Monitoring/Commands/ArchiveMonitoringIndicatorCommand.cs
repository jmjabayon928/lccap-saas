using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Monitoring.Commands;

public sealed class ArchiveMonitoringIndicatorCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveMonitoringIndicatorCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public enum Outcome
    {
        Success,
        NotFound,
        Unauthorized,
    }

    public sealed record Result(Outcome Outcome);

    public async Task<Result> ExecuteAsync(Guid indicatorId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return new Result(Outcome.Unauthorized);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var entity = await _dbContext.MonitoringIndicators
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(
                i => i.Id == indicatorId && i.AccountId == accountId && !i.IsDeleted,
                cancellationToken);

        if (entity is null)
        {
            return new Result(Outcome.NotFound);
        }

        if (entity.Plan.IsDeleted || entity.Plan.AccountId != accountId)
        {
            return new Result(Outcome.NotFound);
        }

        var meta = MonitoringIndicatorMetadataHelper.Parse(entity.MetadataJson);
        var oldValues = JsonSerializer.SerializeToDocument(
            new
            {
                id = entity.Id,
                planId = entity.PlanId,
                actionItemId = entity.ActionItemId,
                name = entity.Name,
                description = entity.Description,
                baselineValue = entity.BaselineValue,
                targetValue = entity.TargetValue,
                unit = entity.Unit,
                status = entity.Status,
                currentValue = meta.CurrentValue,
                progressPercent = meta.ProgressPercent,
                frequency = meta.Frequency,
                responsibleOffice = meta.ResponsibleOffice,
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
            new
            {
                planId = entity.PlanId,
                actionItemId = entity.ActionItemId,
                archiveType = "SoftDelete",
            },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "MonitoringIndicator",
            EntityId = entity.Id,
            Action = "MonitoringIndicatorArchived",
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = _dbContext.AuditLogs.Add(audit);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return new Result(Outcome.Success);
    }
}
