using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Commands;

public class UpdateActionItemCommand
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

    public UpdateActionItemCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<UpdateActionItemOutcome> ExecuteAsync(
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
        var entity = await _dbContext.ActionItems.SingleOrDefaultAsync(
            x => x.Id == actionItemId && x.AccountId == accountId && !x.IsDeleted,
            cancellationToken);

        if (entity is null)
        {
            return UpdateActionItemOutcome.ItemMissingOutcome();
        }

        if (!entity.RowVersion.SequenceEqual(request.RowVersion))
        {
            return UpdateActionItemOutcome.ConcurrencyMismatchOutcome();
        }

        var budget = request.BudgetAmount ?? 0m;
        var status = string.IsNullOrWhiteSpace(request.Status) ? "Planned" : request.Status!.Trim();

        entity.UpdateDetails(
            request.Title!,
            request.Description,
            request.ActionType!.Trim(),
            request.Sector!.Trim(),
            request.ResponsibleOffice,
            budget,
            request.FundingSource,
            request.TimelineStartUtc,
            request.TimelineEndUtc,
            request.Kpi,
            request.PriorityScore,
            status,
            request.MetadataJson ?? JsonDocument.Parse("{}"),
            _currentUserContext.UserId.Value,
            DateTimeOffset.UtcNow);

        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return UpdateActionItemOutcome.OkResult(new ActionItemDto(entity));
    }

    private static List<string> ValidateActionFields(UpdateActionItemRequest request)
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
