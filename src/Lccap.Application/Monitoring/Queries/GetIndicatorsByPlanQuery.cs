using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Pagination;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Monitoring.Queries;

public class GetIndicatorsByPlanQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetIndicatorsByPlanQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<PagedResult<MonitoringIndicator>> ExecuteAsync(
        Guid planId,
        int? page = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new PagedResult<MonitoringIndicator>(Array.Empty<MonitoringIndicator>(), 1, 25, 0);
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planExists = await _dbContext.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);
        if (!planExists)
        {
            return new PagedResult<MonitoringIndicator>(Array.Empty<MonitoringIndicator>(), 1, 25, 0);
        }

        var (p, ps) = PaginationHelper.Normalize(page, pageSize);

        var baseQuery = _dbContext.MonitoringIndicators
            .Where(i => i.AccountId == accountId && i.PlanId == planId && !i.IsDeleted);

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .OrderBy(i => i.Name)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var repaired = false;
        foreach (var row in rows)
        {
            if (row.RowVersion == null || row.RowVersion.Length == 0)
            {
                row.EnsureRowVersion();
                repaired = true;
            }
        }
        if (repaired)
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new PagedResult<MonitoringIndicator>(rows, p, ps, totalCount);
    }
}
