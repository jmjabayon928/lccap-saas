using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Queries;

public sealed class GetActionFundingAllocationsByActionQuery
{
    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public GetActionFundingAllocationsByActionQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetActionFundingAllocationsByActionOutcome> ExecuteAsync(Guid actionItemId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return GetActionFundingAllocationsByActionOutcome.Success(new GetActionFundingAllocationsListResult(Array.Empty<ActionFundingAllocationListItemDto>()));
        }

        var accountId = _currentUserContext.AccountId.Value;

        var actionExists = await _dbContext.ActionItems.AnyAsync(
            a => a.Id == actionItemId && a.AccountId == accountId && !a.IsDeleted,
            cancellationToken);
        if (!actionExists)
        {
            return GetActionFundingAllocationsByActionOutcome.MissingAction();
        }

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
            where alloc.AccountId == accountId
                  && alloc.ActionItemId == actionItemId
                  && !alloc.IsDeleted
            orderby alloc.FiscalYear ascending, fs.Name ascending, alloc.CreatedAtUtc descending
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

        var items = await q.ToListAsync(cancellationToken);

        return GetActionFundingAllocationsByActionOutcome.Success(new GetActionFundingAllocationsListResult(items));
    }
}

public sealed class GetActionFundingAllocationsByActionOutcome
{
    private GetActionFundingAllocationsByActionOutcome(bool ok, bool isActionMissing, GetActionFundingAllocationsListResult? result)
    {
        Ok = ok;
        IsActionMissing = isActionMissing;
        Result = result;
    }

    [JsonIgnore]
    public bool Ok { get; }

    [JsonIgnore]
    public bool IsActionMissing { get; }

    public GetActionFundingAllocationsListResult? Result { get; }

    public static GetActionFundingAllocationsByActionOutcome Success(GetActionFundingAllocationsListResult result) =>
        new(true, false, result);

    public static GetActionFundingAllocationsByActionOutcome MissingAction() =>
        new(false, true, null);
}
