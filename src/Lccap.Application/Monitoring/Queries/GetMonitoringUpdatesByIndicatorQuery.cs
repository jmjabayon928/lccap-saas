using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Monitoring.Queries;

public sealed class GetMonitoringUpdatesByIndicatorQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMonitoringUpdatesByIndicatorQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public enum Outcome
    {
        Success,
        NotFound,
        Unauthorized,
    }

    public sealed record Result(Outcome Outcome, IReadOnlyList<MonitoringUpdateListItem> Items);

    public async Task<Result> ExecuteAsync(Guid indicatorId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new Result(Outcome.Unauthorized, Array.Empty<MonitoringUpdateListItem>());
        }

        var accountId = _currentUserContext.AccountId.Value;

        if (indicatorId == Guid.Empty)
        {
            return new Result(Outcome.NotFound, Array.Empty<MonitoringUpdateListItem>());
        }

        var indicator = await _dbContext.MonitoringIndicators
            .Include(i => i.Plan)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == indicatorId && i.AccountId == accountId && !i.IsDeleted, cancellationToken);

        if (indicator is null)
        {
            return new Result(Outcome.NotFound, Array.Empty<MonitoringUpdateListItem>());
        }

        if (indicator.Plan.IsDeleted || indicator.Plan.AccountId != accountId)
        {
            return new Result(Outcome.NotFound, Array.Empty<MonitoringUpdateListItem>());
        }

        var updates = await _dbContext.MonitoringUpdates
            .AsNoTracking()
            .Where(u => u.MonitoringIndicatorId == indicatorId && u.AccountId == accountId && !u.IsDeleted)
            .OrderByDescending(u => u.ReportedAtUtc)
            .ThenByDescending(u => u.CreatedAtUtc)
            .ThenByDescending(u => u.Id)
            .Select(u => new MonitoringUpdateListItem(
                u.Id,
                u.MonitoringIndicatorId,
                u.PeriodLabel,
                u.ActualValue,
                u.ProgressPercent,
                u.Status,
                u.Notes,
                u.ReportedAtUtc,
                u.ReportedByUserId,
                u.CreatedAtUtc,
                u.CreatedByUserId,
                Convert.ToBase64String(u.RowVersion)))
            .ToListAsync(cancellationToken);

        return new Result(Outcome.Success, updates);
    }

    public sealed record MonitoringUpdateListItem(
        Guid Id,
        Guid MonitoringIndicatorId,
        string PeriodLabel,
        decimal? ActualValue,
        decimal? ProgressPercent,
        string Status,
        string? Notes,
        DateTimeOffset ReportedAtUtc,
        Guid? ReportedByUserId,
        DateTimeOffset CreatedAtUtc,
        Guid? CreatedByUserId,
        string RowVersion);
}

