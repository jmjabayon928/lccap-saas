using Lccap.Application.Actions.Commands;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Queries;

public class GetActionItemsByPlanQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetActionItemsByPlanQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetActionItemsByPlanOutcome> ExecuteAsync(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return GetActionItemsByPlanOutcome.ForbiddenOutcome();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var planExists = await _dbContext.Plans.AnyAsync(
            p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);
        if (!planExists)
        {
            return GetActionItemsByPlanOutcome.PlanMissingOutcome();
        }

        var entities = await _dbContext.ActionItems
            .Where(x => x.AccountId == accountId && x.PlanId == planId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

        var items = entities.Select(x => new ActionItemDto(x)).ToList();

        return GetActionItemsByPlanOutcome.Found(items);
    }
}

public sealed class GetActionItemsByPlanOutcome
{
    private GetActionItemsByPlanOutcome(bool isSuccess, bool forbiddenAccess, bool missingPlan, IReadOnlyList<ActionItemDto>? items)
    {
        IsSuccess = isSuccess;
        ForbiddenAccess = forbiddenAccess;
        MissingPlan = missingPlan;
        Items = items;
    }

    public bool IsSuccess { get; }

    public bool ForbiddenAccess { get; }

    public bool MissingPlan { get; }

    public IReadOnlyList<ActionItemDto>? Items { get; }

    public static GetActionItemsByPlanOutcome Found(IReadOnlyList<ActionItemDto> items) =>
        new(true, false, false, items);

    public static GetActionItemsByPlanOutcome ForbiddenOutcome() =>
        new(false, true, false, null);

    public static GetActionItemsByPlanOutcome PlanMissingOutcome() =>
        new(false, false, true, null);
}
