using Lccap.Application.Actions.Commands;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Pagination;
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

    public async Task<PagedResult<ActionItemDto>> ExecuteAsync(
        Guid planId,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new PagedResult<ActionItemDto>(Array.Empty<ActionItemDto>(), 1, 25, 0);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var planExists = await _dbContext.Plans.AnyAsync(
            p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);
        if (!planExists)
        {
            return new PagedResult<ActionItemDto>(Array.Empty<ActionItemDto>(), 1, 25, 0);
        }

        var (p, ps) = PaginationHelper.Normalize(page, pageSize);

        var baseQuery = _dbContext.ActionItems
            .Where(x => x.AccountId == accountId && x.PlanId == planId && !x.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var entities = await baseQuery
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Title)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var repaired = false;
        foreach (var entity in entities)
        {
            if (entity.RowVersion == null || entity.RowVersion.Length == 0)
            {
                entity.EnsureRowVersion();
                repaired = true;
            }
        }

        if (repaired)
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var items = entities.Select(x => new ActionItemDto(x)).ToList();

        return new PagedResult<ActionItemDto>(items, p, ps, totalCount);
    }
}
