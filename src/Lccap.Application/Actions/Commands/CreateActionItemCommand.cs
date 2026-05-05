using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Commands;

public class CreateActionItemCommand
{
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

    public CreateActionItemCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreateActionItemOutcome> ExecuteAsync(
        Guid planId,
        CreateActionItemRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return CreateActionItemOutcome.ForbiddenOutcome();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var errors = ValidateActionFields(request);
        if (errors.Count > 0)
        {
            return CreateActionItemOutcome.ValidationFailure(errors);
        }

        var planExists = await _dbContext.Plans.AnyAsync(
            p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);
        if (!planExists)
        {
            return CreateActionItemOutcome.PlanMissingOutcome();
        }

        var budget = request.BudgetAmount ?? 0m;
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Planned" : request.Status!.Trim();

        var now = DateTimeOffset.UtcNow;
        var item = new ActionItem
        {
            AccountId = accountId,
            PlanId = planId,
            Title = request.Title.Trim(),
            Description = request.Description,
            ActionType = request.ActionType.Trim(),
            Sector = request.Sector.Trim(),
            ResponsibleOffice = request.ResponsibleOffice,
            BudgetAmount = budget,
            FundingSource = request.FundingSource,
            TimelineStartUtc = request.TimelineStartUtc,
            TimelineEndUtc = request.TimelineEndUtc,
            Kpi = request.Kpi,
            PriorityScore = request.PriorityScore,
            Status = status,
            MetadataJson = request.MetadataJson ?? JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            CreatedByUserId = _currentUserContext.UserId,
            IsDeleted = false,
        };
        item.EnsureRowVersion();

        _ = _dbContext.ActionItems.Add(item);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateActionItemOutcome.Created(new ActionItemDto(item));
    }

    private List<string> ValidateActionFields(CreateActionItemRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title must not be blank.");
        }

        if (string.IsNullOrWhiteSpace(request.ActionType) || !AllowedActionTypes.Contains(request.ActionType.Trim()))
        {
            errors.Add("Action type is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Sector))
        {
            errors.Add("Sector must not be blank.");
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

        if (!string.IsNullOrWhiteSpace(request.Status) && !AllowedStatuses.Contains(request.Status.Trim()))
        {
            errors.Add("Status is invalid.");
        }

        return errors;
    }
}

public sealed record CreateActionItemRequest(
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
    JsonDocument? MetadataJson);

public sealed class CreateActionItemOutcome
{
    private CreateActionItemOutcome(bool isSuccess, int statusCode, ActionItemDto? item, bool forbiddenAccess, bool missingPlan, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Item = item;
        ForbiddenAccess = forbiddenAccess;
        MissingPlan = missingPlan;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public ActionItemDto? Item { get; }

    public bool ForbiddenAccess { get; }

    public bool MissingPlan { get; }

    public IReadOnlyList<string> Errors { get; }

    public static CreateActionItemOutcome Created(ActionItemDto item) =>
        new(true, 201, item, false, false, Array.Empty<string>());

    public static CreateActionItemOutcome ValidationFailure(IReadOnlyList<string> errors) =>
        new(false, 400, null, false, false, errors);

    public static CreateActionItemOutcome ForbiddenOutcome() =>
        new(false, 403, null, true, false, Array.Empty<string>());

    public static CreateActionItemOutcome PlanMissingOutcome() =>
        new(false, 404, null, false, true, Array.Empty<string>());
}

public sealed record ActionItemDto(
    Guid Id,
    Guid AccountId,
    Guid PlanId,
    string Title,
    string? Description,
    string ActionType,
    string Sector,
    string? ResponsibleOffice,
    decimal BudgetAmount,
    string? FundingSource,
    DateTimeOffset? TimelineStartUtc,
    DateTimeOffset? TimelineEndUtc,
    string? Kpi,
    decimal? PriorityScore,
    string Status,
    JsonDocument MetadataJson,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId,
    byte[] RowVersion)
{
    public ActionItemDto(ActionItem item)
        : this(
            item.Id,
            item.AccountId,
            item.PlanId,
            item.Title,
            item.Description,
            item.ActionType,
            item.Sector,
            item.ResponsibleOffice,
            item.BudgetAmount,
            item.FundingSource,
            item.TimelineStartUtc,
            item.TimelineEndUtc,
            item.Kpi,
            item.PriorityScore,
            item.Status,
            item.MetadataJson,
            item.CreatedAtUtc,
            item.UpdatedAtUtc,
            item.CreatedByUserId,
            item.UpdatedByUserId,
            item.RowVersion)
    {
    }
}
