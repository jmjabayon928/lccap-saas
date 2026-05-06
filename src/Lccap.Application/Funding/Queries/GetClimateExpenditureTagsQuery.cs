using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Queries;

public sealed class GetClimateExpenditureTagsQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetClimateExpenditureTagsQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetClimateExpenditureTagsResult> ExecuteAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new GetClimateExpenditureTagsResult(Array.Empty<ClimateExpenditureTagListItemDto>(), 0, includeInactive);
        }

        var accountId = _currentUserContext.AccountId.Value;

        var queryable = _dbContext.ClimateExpenditureTags
            .AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted);

        if (!includeInactive)
        {
            queryable = queryable.Where(x => x.IsActive);
        }

        var list = await queryable
            .OrderBy(x => x.TagCategory)
            .ThenBy(x => x.TagCode)
            .ThenBy(x => x.TagName)
            .Select(x => new ClimateExpenditureTagListItemDto(
                x.Id,
                x.TagCode,
                x.TagName,
                x.TagCategory,
                x.WeightPercent,
                x.Description,
                x.IsActive,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new GetClimateExpenditureTagsResult(list, list.Count, includeInactive);
    }
}

public sealed record ClimateExpenditureTagListItemDto(
    Guid Id,
    string TagCode,
    string TagName,
    string TagCategory,
    decimal? WeightPercent,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

public sealed record GetClimateExpenditureTagsResult(
    IReadOnlyList<ClimateExpenditureTagListItemDto> Items,
    int TotalCount,
    bool IncludeInactive);
