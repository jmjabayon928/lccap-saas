using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Queries;

public sealed class GetFundingSourcesQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetFundingSourcesQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetFundingSourcesResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new GetFundingSourcesResult(Array.Empty<FundingSourceListItemDto>(), 0);
        }

        var accountId = _currentUserContext.AccountId.Value;

        var list = await _dbContext.FundingSources
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted)
            .OrderBy(x => x.SourceType)
            .ThenBy(x => x.Name)
            .Select(x => new FundingSourceListItemDto(
                x.Id,
                x.Name,
                x.SourceType,
                x.Description,
                x.ContactName,
                x.ContactEmail,
                x.WebsiteUrl,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new GetFundingSourcesResult(list, list.Count);
    }
}

public sealed record FundingSourceListItemDto(
    Guid Id,
    string Name,
    string SourceType,
    string? Description,
    string? ContactName,
    string? ContactEmail,
    string? WebsiteUrl,
    DateTimeOffset CreatedAtUtc);

public sealed record GetFundingSourcesResult(
    IReadOnlyList<FundingSourceListItemDto> Items,
    int TotalCount);
