using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Commands;

public class UpdateActionItemCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> AllowedActionTypes = new(StringComparer.Ordinal)
    {
        "Adaptation",
        "Mitigation",
    };

    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "Planned",
        "InProgress",
        "OnTrack",
        "Delayed",
        "Completed",
        "Cancelled",
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdateActionItemCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<UpdateActionItemOutcome> ExecuteAsync(
        Guid actionItemId,
        UpdateActionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return UpdateActionItemOutcome.ForbiddenOutcome();
        }

        var validationErrors = ValidateActionFields(request);
        if (validationErrors.Count > 0)
        {
            return UpdateActionItemOutcome.ValidationFailure(validationErrors);
        }

        if (request.RowVersion is null || request.RowVersion.Length == 0)
        {
            return UpdateActionItemOutcome.ValidationFailure(new[] { "Row version is required." });
        }

        var accountId = _currentUserContext.AccountId.Value;
        var entity = await _dbContext.ActionItems
            .Include(x => x.Plan)
            .SingleOrDefaultAsync(
                x => x.Id == actionItemId && x.AccountId == accountId && !x.IsDeleted,
                cancellationToken);

        if (entity is null)
        {
            return UpdateActionItemOutcome.ItemMissingOutcome();
        }

        if (entity.Plan.IsDeleted || entity.Plan.AccountId != accountId)
        {
            return UpdateActionItemOutcome.ItemMissingOutcome();
        }

        if (!entity.RowVersion.SequenceEqual(request.RowVersion))
        {
            return UpdateActionItemOutcome.ConcurrencyMismatchOutcome();
        }

        var budget = request.BudgetAmount ?? 0m;
        var status = request.Status!.Trim();

        var oldSnapshot = BuildFieldSnapshot(
            entity.Title,
            entity.Description,
            entity.ActionType,
            entity.Sector,
            entity.ResponsibleOffice,
            entity.BudgetAmount,
            entity.FundingSource,
            entity.TimelineStartUtc,
            entity.TimelineEndUtc,
            entity.Kpi,
            entity.PriorityScore,
            entity.Status);

        entity.UpdateDetails(
            request.Title!.Trim(),
            NormalizeOptional(request.Description),
            request.ActionType!.Trim(),
            request.Sector!.Trim(),
            NormalizeOptional(request.ResponsibleOffice),
            budget,
            NormalizeOptional(request.FundingSource),
            request.TimelineStartUtc,
            request.TimelineEndUtc,
            NormalizeOptional(request.Kpi),
            request.PriorityScore,
            status,
            _currentUserContext.UserId.Value,
            DateTimeOffset.UtcNow);

        var newSnapshot = BuildFieldSnapshot(
            entity.Title,
            entity.Description,
            entity.ActionType,
            entity.Sector,
            entity.ResponsibleOffice,
            entity.BudgetAmount,
            entity.FundingSource,
            entity.TimelineStartUtc,
            entity.TimelineEndUtc,
            entity.Kpi,
            entity.PriorityScore,
            entity.Status);

        var metadata = JsonSerializer.SerializeToDocument(new { planId = entity.PlanId }, AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = _currentUserContext.UserId,
            EntityName = "ActionItem",
            EntityId = entity.Id,
            Action = "ActionItemUpdated",
            OldValuesJson = oldSnapshot,
            NewValuesJson = newSnapshot,
            MetadataJson = metadata,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };

        _ = _dbContext.AuditLogs.Add(audit);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateActionItemOutcome.OkResult(new ActionItemDto(entity));
    }

    private static JsonDocument BuildFieldSnapshot(
        string title,
        string? description,
        string actionType,
        string sector,
        string? responsibleOffice,
        decimal budgetAmount,
        string? fundingSource,
        DateTimeOffset? timelineStartUtc,
        DateTimeOffset? timelineEndUtc,
        string? kpi,
        decimal? priorityScore,
        string status)
    {
        return JsonSerializer.SerializeToDocument(
            new
            {
                title,
                description,
                actionType,
                sector,
                responsibleOffice,
                budgetAmount,
                fundingSource,
                timelineStartUtc,
                timelineEndUtc,
                kpi,
                priorityScore,
                status,
            },
            AuditJsonOptions);
    }

    private static string? NormalizeOptional(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var t = value.Trim();
        return t.Length == 0 ? null : t;
    }

    private static List<string> ValidateActionFields(UpdateActionItemRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title must not be blank.");
        }
        else if (request.Title.Trim().Length > 250)
        {
            errors.Add("Title must be 250 characters or fewer.");
        }

        if (string.IsNullOrWhiteSpace(request.ActionType) || !AllowedActionTypes.Contains(request.ActionType.Trim()))
        {
            errors.Add("Action type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Sector))
        {
            errors.Add("Sector must not be blank.");
        }
        else if (request.Sector.Trim().Length > 100)
        {
            errors.Add("Sector must be 100 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(request.ResponsibleOffice) && request.ResponsibleOffice.Trim().Length > 150)
        {
            errors.Add("Responsible office must be 150 characters or fewer.");
        }

        if (!string.IsNullOrWhiteSpace(request.FundingSource) && request.FundingSource.Trim().Length > 150)
        {
            errors.Add("Funding source must be 150 characters or fewer.");
        }

        var budget = request.BudgetAmount ?? 0m;
        if (budget < 0)
        {
            errors.Add("Budget amount cannot be negative.");
        }

        if (request.TimelineStartUtc.HasValue
            && request.TimelineEndUtc.HasValue
            && request.TimelineStartUtc.Value > request.TimelineEndUtc.Value)
        {
            errors.Add("Timeline start cannot be after timeline end.");
        }

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            errors.Add("Status must not be blank.");
        }
        else if (!AllowedStatuses.Contains(request.Status.Trim()))
        {
            errors.Add("Status is invalid.");
        }

        if (request.PriorityScore.HasValue)
        {
            var p = request.PriorityScore.Value;
            if (p < 0m || p > 100m)
            {
                errors.Add("Priority score must be between 0 and 100 when provided.");
            }
        }

        return errors;
    }
}

public sealed record UpdateActionItemRequest(
    string Title,
    string? Description,
    string ActionType,
    string Sector,
    string? ResponsibleOffice,
    decimal? BudgetAmount,
    string? FundingSource,
    DateTimeOffset? TimelineStartUtc,
    DateTimeOffset? TimelineEndUtc,
    string? Kpi,
    decimal? PriorityScore,
    string? Status,
    JsonDocument? MetadataJson,
    byte[]? RowVersion);

public sealed class UpdateActionItemOutcome
{
    private UpdateActionItemOutcome(
        bool isSuccess,
        int statusCode,
        ActionItemDto? item,
        bool forbiddenAccess,
        bool missingItem,
        bool concurrencyMismatch,
        IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Item = item;
        ForbiddenAccess = forbiddenAccess;
        MissingItem = missingItem;
        ConcurrencyStale = concurrencyMismatch;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public ActionItemDto? Item { get; }

    public bool ForbiddenAccess { get; }

    public bool MissingItem { get; }

    public bool ConcurrencyStale { get; }

    public IReadOnlyList<string> Errors { get; }

    public static UpdateActionItemOutcome OkResult(ActionItemDto item) => new(true, 200, item, false, false, false, Array.Empty<string>());

    public static UpdateActionItemOutcome ValidationFailure(IReadOnlyList<string> errors) =>
        new(false, 400, null, false, false, false, errors);

    public static UpdateActionItemOutcome ForbiddenOutcome() =>
        new(false, 403, null, true, false, false, Array.Empty<string>());

    public static UpdateActionItemOutcome ItemMissingOutcome() =>
        new(false, 404, null, false, true, false, Array.Empty<string>());

    public static UpdateActionItemOutcome ConcurrencyMismatchOutcome() =>
        new(false, 409, null, false, false, true, Array.Empty<string>());
}
