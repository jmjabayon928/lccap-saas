using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Queries;

/// <summary>
/// List projection for GET /api/plans — excludes concurrency tokens and non-list metadata.
/// </summary>
public sealed record PlanListItemDto(
    Guid Id,
    Guid AccountId,
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed class GetPlansQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlansQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<PagedResult<PlanListItemDto>> Execute(int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || _currentUserContext.AccountId is null)
        {
            return new PagedResult<PlanListItemDto>(Array.Empty<PlanListItemDto>(), 1, 25, 0);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var (p, ps) = PaginationHelper.Normalize(page, pageSize);

        var baseQuery = _dbContext.Plans
            .AsNoTracking()
            .Where(p => p.AccountId == accountId && !p.IsDeleted && p.Status != "Archived");

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var items = await baseQuery
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Select(p => new PlanListItemDto(
                p.Id,
                p.AccountId,
                p.Title,
                p.StartYear,
                p.EndYear,
                p.Status,
                p.TemplateMode,
                p.VersionNumber,
                p.Description,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        return new PagedResult<PlanListItemDto>(items, p, ps, totalCount);
    }
}
