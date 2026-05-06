using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Queries;

public sealed class GetFundingProgramsQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetFundingProgramsQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetFundingProgramsQueryOutcome> ExecuteAsync(
        Guid? fundingSourceId,
        bool includeInactiveOrClosed,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            var emptyEarly = EmptyResult(fundingSourceId, includeInactiveOrClosed);
            return new GetFundingProgramsQueryOutcome(false, emptyEarly);
        }

        var accountId = _currentUserContext.AccountId.Value;

        if (fundingSourceId.HasValue)
        {
            var sourceOk = await _dbContext.FundingSources
                .AsNoTracking()
                .AnyAsync(
                    x => x.Id == fundingSourceId.Value && x.AccountId == accountId && !x.IsDeleted,
                    cancellationToken);

            if (!sourceOk)
            {
                return new GetFundingProgramsQueryOutcome(true, null);
            }
        }

        var queryable =
            from fp in _dbContext.FundingPrograms.AsNoTracking()
            join fs in _dbContext.FundingSources.AsNoTracking() on fp.FundingSourceId equals fs.Id
            where fp.AccountId == accountId
                  && !fp.IsDeleted
                  && fs.AccountId == accountId
                  && !fs.IsDeleted
            select new { fp, fs };

        if (!includeInactiveOrClosed)
        {
            queryable = queryable.Where(x => x.fp.Status == "Active");
        }

        if (fundingSourceId.HasValue)
        {
            var sid = fundingSourceId.Value;
            queryable = queryable.Where(x => x.fp.FundingSourceId == sid);
        }

        var list = await queryable
            .OrderBy(x => x.fs.Name)
            .ThenBy(x => x.fp.Status)
            .ThenBy(x => x.fp.Name)
            .Select(x => new FundingProgramListItemDto(
                x.fp.Id,
                x.fp.FundingSourceId,
                x.fs.Name,
                x.fp.Name,
                x.fp.ProgramCode,
                x.fp.Description,
                x.fp.EligibleUses,
                x.fp.ApplicationUrl,
                x.fp.OpensAtUtc,
                x.fp.ClosesAtUtc,
                x.fp.MaxAwardAmount,
                x.fp.CurrencyCode,
                x.fp.Status,
                x.fp.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        var result = new GetFundingProgramsResult(list, list.Count, fundingSourceId, includeInactiveOrClosed);
        return new GetFundingProgramsQueryOutcome(false, result);
    }

    private static GetFundingProgramsResult EmptyResult(Guid? fundingSourceId, bool includeInactiveOrClosed) =>
        new(Array.Empty<FundingProgramListItemDto>(), 0, fundingSourceId, includeInactiveOrClosed);
}

public sealed record FundingProgramListItemDto(
    Guid Id,
    Guid FundingSourceId,
    string FundingSourceName,
    string Name,
    string? ProgramCode,
    string? Description,
    string? EligibleUses,
    string? ApplicationUrl,
    DateTimeOffset? OpensAtUtc,
    DateTimeOffset? ClosesAtUtc,
    decimal? MaxAwardAmount,
    string CurrencyCode,
    string Status,
    DateTimeOffset CreatedAtUtc);

public sealed record GetFundingProgramsQueryOutcome(bool SourceNotFound, GetFundingProgramsResult? Result);

public sealed record GetFundingProgramsResult(
    IReadOnlyList<FundingProgramListItemDto> Items,
    int TotalCount,
    Guid? FundingSourceId,
    bool IncludeInactiveOrClosed);
