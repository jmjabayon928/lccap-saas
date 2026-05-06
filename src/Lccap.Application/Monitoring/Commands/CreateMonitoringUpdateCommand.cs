using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Concurrency;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Monitoring.Commands;

public sealed class CreateMonitoringUpdateCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateMonitoringUpdateCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public sealed record Request(
        Guid IndicatorId,
        string PeriodLabel,
        decimal? ActualValue,
        decimal? ProgressPercent,
        string Status,
        string? Notes);

    public enum Outcome
    {
        Success,
        NotFound,
        Unauthorized,
        Concurrency,
        ValidationFailed,
    }

    public sealed record Result(Outcome Outcome, string? ValidationMessage, MonitoringUpdate? Update);

    public async Task<Result> ExecuteAsync(Request request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return new Result(Outcome.Unauthorized, null, null);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        if (request.IndicatorId == Guid.Empty)
        {
            return new Result(Outcome.ValidationFailed, "Indicator id is required.", null);
        }

        var periodLabel = request.PeriodLabel.Trim();
        if (periodLabel.Length == 0 || periodLabel.Length > 100)
        {
            return new Result(Outcome.ValidationFailed, "Period label must be between 1 and 100 characters.", null);
        }

        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        if (notes is not null && notes.Length > 2000)
        {
            return new Result(Outcome.ValidationFailed, "Notes must be 2000 characters or fewer.", null);
        }

        if (!MonitoringIndicator.IsValidStatus(request.Status))
        {
            return new Result(Outcome.ValidationFailed, "Update status is invalid.", null);
        }

        if (request.ProgressPercent is < 0m or > 100m)
        {
            return new Result(Outcome.ValidationFailed, "Progress percent must be between 0 and 100 when provided.", null);
        }

        var indicator = await _dbContext.MonitoringIndicators
            .Include(i => i.Plan)
            .FirstOrDefaultAsync(
                i => i.Id == request.IndicatorId && i.AccountId == accountId && !i.IsDeleted,
                cancellationToken);

        if (indicator is null)
        {
            return new Result(Outcome.NotFound, null, null);
        }

        if (indicator.Plan.IsDeleted || indicator.Plan.AccountId != accountId)
        {
            return new Result(Outcome.NotFound, null, null);
        }

        var now = DateTimeOffset.UtcNow;

        var metaBefore = MonitoringIndicatorMetadataHelper.Parse(indicator.MetadataJson);
        var currentValueForMerge = request.ActualValue ?? metaBefore.CurrentValue;
        var progressPercentForMerge = request.ProgressPercent ?? metaBefore.ProgressPercent;

        var update = new MonitoringUpdate
        {
            AccountId = accountId,
            MonitoringIndicatorId = indicator.Id,
            PeriodLabel = periodLabel,
            ActualValue = request.ActualValue,
            ProgressPercent = request.ProgressPercent,
            Status = request.Status,
            Notes = notes,
            ReportedAtUtc = now,
            ReportedByUserId = userId,
            CreatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedAtUtc = now,
            UpdatedByUserId = userId,
            IsDeleted = false,
        };
        update.EnsureRowVersion();

        indicator.Status = request.Status;
        indicator.MetadataJson = MonitoringIndicatorMetadataHelper.Merge(
            indicator.MetadataJson,
            currentValueForMerge,
            progressPercentForMerge,
            frequency: metaBefore.Frequency,
            responsibleOffice: metaBefore.ResponsibleOffice);
        indicator.UpdatedAtUtc = now;
        indicator.UpdatedByUserId = userId;
        indicator.RotateRowVersion();

        var auditMetadata = JsonSerializer.SerializeToDocument(
            new
            {
                planId = indicator.PlanId,
                actionItemId = indicator.ActionItemId,
                monitoringIndicatorId = indicator.Id,
                monitoringUpdateId = update.Id,
            },
            AuditJsonOptions);

        var auditNew = JsonSerializer.SerializeToDocument(
            new
            {
                periodLabel = update.PeriodLabel,
                actualValue = update.ActualValue,
                progressPercent = update.ProgressPercent,
                status = update.Status,
                notes = update.Notes,
                reportedAtUtc = update.ReportedAtUtc,
                reportedByUserId = update.ReportedByUserId,
            },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            AccountId = accountId,
            UserId = userId,
            EntityName = "MonitoringUpdate",
            EntityId = update.Id,
            Action = "MonitoringUpdateCreated",
            OldValuesJson = null,
            NewValuesJson = auditNew,
            MetadataJson = auditMetadata,
            CreatedAtUtc = now,
        };
        audit.EnsureRowVersion();

        _ = _dbContext.MonitoringUpdates.Add(update);
        _ = _dbContext.AuditLogs.Add(audit);

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return new Result(Outcome.Concurrency, null, null);
        }

        await NotificationRecipientResolver.TryPublishWorkspaceEventAsync(
            _dbContext,
            _currentUserContext,
            clock: null,
            "MonitoringUpdateCreated",
            "Monitoring update",
            "A monitoring progress update was recorded.",
            "MonitoringUpdate",
            update.Id,
            indicator.PlanId,
            cancellationToken);

        return new Result(Outcome.Success, null, update);
    }
}

