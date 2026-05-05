using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Concurrency;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Monitoring.Commands;

public sealed class UpdateMonitoringIndicatorCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateMonitoringIndicatorCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public sealed record Request(
        Guid IndicatorId,
        string Name,
        string? Description,
        string? Unit,
        decimal? BaselineValue,
        decimal? TargetValue,
        decimal? CurrentValue,
        decimal? ProgressPercent,
        string? Frequency,
        string? ResponsibleOffice,
        string Status,
        byte[] RowVersion);

    public enum Outcome
    {
        Success,
        NotFound,
        Unauthorized,
        Concurrency,
        ValidationFailed,
    }

    public sealed record Result(Outcome Outcome, string? ValidationMessage, MonitoringIndicator? Indicator);

    public async Task<Result> ExecuteAsync(Request request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return new Result(Outcome.Unauthorized, null, null);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var name = request.Name.Trim();
        if (name.Length == 0 || name.Length > 250)
        {
            return new Result(Outcome.ValidationFailed, "Indicator name must be between 1 and 250 characters.", null);
        }

        if (!MonitoringIndicator.IsValidStatus(request.Status))
        {
            return new Result(Outcome.ValidationFailed, "Indicator status is invalid.", null);
        }

        if (request.ProgressPercent is < 0m or > 100m)
        {
            return new Result(Outcome.ValidationFailed, "Progress percent must be between 0 and 100 when provided.", null);
        }

        var unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
        if (unit is not null && unit.Length > 80)
        {
            return new Result(Outcome.ValidationFailed, "Unit must be 80 characters or fewer.", null);
        }

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        var frequency = string.IsNullOrWhiteSpace(request.Frequency) ? null : request.Frequency.Trim();
        var responsibleOffice = string.IsNullOrWhiteSpace(request.ResponsibleOffice) ? null : request.ResponsibleOffice.Trim();

        var entity = await _dbContext.MonitoringIndicators
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(
                i => i.Id == request.IndicatorId && i.AccountId == accountId && !i.IsDeleted,
                cancellationToken);

        if (entity is null)
        {
            return new Result(Outcome.NotFound, null, null);
        }

        if (entity.Plan.IsDeleted || entity.Plan.AccountId != accountId)
        {
            return new Result(Outcome.NotFound, null, null);
        }

        var metaBefore = MonitoringIndicatorMetadataHelper.Parse(entity.MetadataJson);
        var oldValues = JsonSerializer.SerializeToDocument(
            new
            {
                name = entity.Name,
                description = entity.Description,
                baselineValue = entity.BaselineValue,
                targetValue = entity.TargetValue,
                unit = entity.Unit,
                status = entity.Status,
                currentValue = metaBefore.CurrentValue,
                progressPercent = metaBefore.ProgressPercent,
                frequency = metaBefore.Frequency,
                responsibleOffice = metaBefore.ResponsibleOffice,
            },
            AuditJsonOptions);

        var mergedMetadata = MonitoringIndicatorMetadataHelper.Merge(
            entity.MetadataJson,
            request.CurrentValue,
            request.ProgressPercent,
            frequency,
            responsibleOffice);

        if (!entity.RowVersion.SequenceEqual(request.RowVersion))
        {
            return new Result(Outcome.Concurrency, null, null);
        }

        var now = DateTimeOffset.UtcNow;
        entity.UpdateDefinition(
            name,
            description,
            request.BaselineValue,
            request.TargetValue,
            unit,
            request.Status,
            mergedMetadata,
            userId,
            now);

        entity.RotateRowVersion();

        var metaAfter = MonitoringIndicatorMetadataHelper.Parse(entity.MetadataJson);
        var newValues = JsonSerializer.SerializeToDocument(
            new
            {
                name = entity.Name,
                description = entity.Description,
                baselineValue = entity.BaselineValue,
                targetValue = entity.TargetValue,
                unit = entity.Unit,
                status = entity.Status,
                currentValue = metaAfter.CurrentValue,
                progressPercent = metaAfter.ProgressPercent,
                frequency = metaAfter.Frequency,
                responsibleOffice = metaAfter.ResponsibleOffice,
            },
            AuditJsonOptions);

        var auditMetadata = JsonSerializer.SerializeToDocument(
            new
            {
                planId = entity.PlanId,
                actionItemId = entity.ActionItemId,
            },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "MonitoringIndicator",
            EntityId = entity.Id,
            Action = "MonitoringIndicatorUpdated",
            OldValuesJson = oldValues,
            NewValuesJson = newValues,
            MetadataJson = auditMetadata,
            CreatedAtUtc = now,
            RowVersion = RowVersionHelper.NewRowVersion(),
        };

        _ = _dbContext.AuditLogs.Add(audit);

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Result(Outcome.Concurrency, null, null);
        }

        return new Result(Outcome.Success, null, entity);
    }
}
