using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Queries;

public sealed record ActionFundingAllocationListItemDto(
    Guid Id,
    Guid PlanId,
    Guid ActionItemId,
    string ActionTitle,
    Guid FundingSourceId,
    string FundingSourceName,
    Guid? FundingProgramId,
    string? FundingProgramName,
    Guid? ClimateExpenditureTagId,
    string? ClimateExpenditureTagCode,
    string? ClimateExpenditureTagName,
    string? ClimateExpenditureTagCategory,
    int FiscalYear,
    decimal AllocatedAmount,
    string CurrencyCode,
    string AllocationStatus,
    string? Notes,
    DateTimeOffset CreatedAtUtc);

/// <summary>Result shape for allocations listed by plan or action item.</summary>
public sealed record GetActionFundingAllocationsListResult(IReadOnlyList<ActionFundingAllocationListItemDto> Items);

public sealed class GetActionFundingAllocationsByPlanQuery
{
    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public GetActionFundingAllocationsByPlanQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetActionFundingAllocationsByPlanOutcome> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return GetActionFundingAllocationsByPlanOutcome.Success(new GetActionFundingAllocationsListResult(Array.Empty<ActionFundingAllocationListItemDto>()));
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planExists = await _dbContext.Plans.AnyAsync(
            p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);
        if (!planExists)
        {
            return GetActionFundingAllocationsByPlanOutcome.MissingPlan();
        }

        var items = await BuildOrderedQuery(accountId, planId: planId)
            .ToListAsync(cancellationToken);

        return GetActionFundingAllocationsByPlanOutcome.Success(new GetActionFundingAllocationsListResult(items));
    }

    /// <remarks>Queryable ordered by fiscal year, action title, funding source name, then newest created first.</remarks>
    internal IQueryable<ActionFundingAllocationListItemDto> BuildOrderedQuery(Guid accountId, Guid planId)
    {
        var q =
            from alloc in _dbContext.ActionFundingAllocations.AsNoTracking()
            join actionItem in _dbContext.ActionItems.AsNoTracking()
                on alloc.ActionItemId equals actionItem.Id
            join fs in _dbContext.FundingSources.AsNoTracking()
                on alloc.FundingSourceId equals fs.Id
            join progJoin in _dbContext.FundingPrograms.AsNoTracking()
                on alloc.FundingProgramId equals progJoin.Id into progGroup
            from prog in progGroup.DefaultIfEmpty()
            join tagJoin in _dbContext.ClimateExpenditureTags.AsNoTracking()
                on alloc.ClimateExpenditureTagId equals tagJoin.Id into tagGroup
            from ccet in tagGroup.DefaultIfEmpty()
            where alloc.AccountId == accountId && alloc.PlanId == planId && !alloc.IsDeleted
            orderby alloc.FiscalYear ascending, actionItem.Title ascending, fs.Name ascending, alloc.CreatedAtUtc descending
            select new ActionFundingAllocationListItemDto(
                alloc.Id,
                alloc.PlanId,
                alloc.ActionItemId,
                actionItem.Title,
                alloc.FundingSourceId,
                fs.Name,
                alloc.FundingProgramId,
                prog != null ? prog.Name : null,
                alloc.ClimateExpenditureTagId,
                ccet != null ? ccet.TagCode : null,
                ccet != null ? ccet.TagName : null,
                ccet != null ? ccet.TagCategory : null,
                alloc.FiscalYear,
                alloc.AllocatedAmount,
                alloc.CurrencyCode,
                alloc.AllocationStatus,
                alloc.Notes,
                alloc.CreatedAtUtc);

        return q;
    }
}

public sealed class GetActionFundingAllocationsByPlanOutcome
{
    private GetActionFundingAllocationsByPlanOutcome(bool ok, bool isPlanMissing, GetActionFundingAllocationsListResult? result)
    {
        Ok = ok;
        IsPlanMissing = isPlanMissing;
        Result = result;
    }

    [JsonIgnore]
    public bool Ok { get; }

    [JsonIgnore]
    public bool IsPlanMissing { get; }

    public GetActionFundingAllocationsListResult? Result { get; }

    public static GetActionFundingAllocationsByPlanOutcome Success(GetActionFundingAllocationsListResult result) =>
        new(true, false, result);

    public static GetActionFundingAllocationsByPlanOutcome MissingPlan() =>
        new(false, true, null);
}
