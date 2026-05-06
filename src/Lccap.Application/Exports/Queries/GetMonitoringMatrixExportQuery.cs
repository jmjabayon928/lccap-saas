using Lccap.Application.Common.Interfaces;
using Lccap.Application.Exports;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Exports.Queries;

public sealed class GetMonitoringMatrixExportQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetMonitoringMatrixExportQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<PlanExportCsvResult> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.AccountId.HasValue)
        {
            return PlanExportCsvResult.UnauthenticatedAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planExists = await _dbContext.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);

        if (!planExists)
        {
            return PlanExportCsvResult.MissingPlan();
        }

        var indicators = await (
                from mi in _dbContext.MonitoringIndicators.AsNoTracking()
                join a in _dbContext.ActionItems.AsNoTracking()
                        .Where(x => x.AccountId == accountId && x.PlanId == planId && !x.IsDeleted)
                    on mi.ActionItemId equals a.Id into actionJoin
                from a in actionJoin.DefaultIfEmpty()
                where mi.AccountId == accountId && mi.PlanId == planId && !mi.IsDeleted
                orderby mi.Status, mi.Name
                select new
                {
                    mi.Id,
                    ActionItemId = mi.ActionItemId,
                    ActionTitle = a != null ? a.Title : null,
                    mi.Name,
                    mi.Description,
                    mi.BaselineValue,
                    mi.TargetValue,
                    mi.Unit,
                    mi.Status,
                    mi.CreatedAtUtc
                })
            .ToListAsync(cancellationToken);

        if (indicators.Count == 0)
        {
            var emptyHeaders = new[]
            {
                "IndicatorId",
                "ActionItemId",
                "ActionTitle",
                "Name",
                "Description",
                "BaselineValue",
                "TargetValue",
                "Unit",
                "Status",
                "LatestPeriodLabel",
                "LatestActualValue",
                "LatestProgressPercent",
                "LatestUpdateStatus",
                "LatestReportedAtUtc",
                "LatestNotes",
                "CreatedAtUtc"
            };
            return PlanExportCsvResult.Ok(CsvExportFormatter.Build(emptyHeaders, []));
        }

        var indicatorIds = indicators.ConvertAll(i => i.Id);

        var updates = await _dbContext.MonitoringUpdates
            .AsNoTracking()
            .Where(u => u.AccountId == accountId && !u.IsDeleted && indicatorIds.Contains(u.MonitoringIndicatorId))
            .Select(
                u => new
                {
                    u.MonitoringIndicatorId,
                    u.PeriodLabel,
                    u.ActualValue,
                    u.ProgressPercent,
                    u.Status,
                    u.ReportedAtUtc,
                    u.Notes,
                    u.CreatedAtUtc
                })
            .ToListAsync(cancellationToken);

        var latestByIndicator =
            new Dictionary<Guid, (string PeriodLabel, decimal? ActualValue, decimal? ProgressPercent, string Status, DateTimeOffset ReportedAtUtc, string? Notes)>();
        foreach (var g in updates.GroupBy(u => u.MonitoringIndicatorId))
        {
            var best = g.OrderByDescending(x => x.ReportedAtUtc).ThenByDescending(x => x.CreatedAtUtc).First();
            latestByIndicator[g.Key] = (
                best.PeriodLabel,
                best.ActualValue,
                best.ProgressPercent,
                best.Status,
                best.ReportedAtUtc,
                best.Notes);
        }

        var headers = new[]
        {
            "IndicatorId",
            "ActionItemId",
            "ActionTitle",
            "Name",
            "Description",
            "BaselineValue",
            "TargetValue",
            "Unit",
            "Status",
            "LatestPeriodLabel",
            "LatestActualValue",
            "LatestProgressPercent",
            "LatestUpdateStatus",
            "LatestReportedAtUtc",
            "LatestNotes",
            "CreatedAtUtc"
        };

        var lines = new List<IReadOnlyList<string>>(indicators.Count);
        foreach (var mi in indicators)
        {
            DateTimeOffset? reportedUtc = null;
            string latestPeriodLabel = string.Empty;
            string latestActual = string.Empty;
            string latestProgress = string.Empty;
            string latestStatus = string.Empty;
            string latestNotes = string.Empty;
            if (latestByIndicator.TryGetValue(mi.Id, out var latest))
            {
                latestPeriodLabel = latest.PeriodLabel;
                latestActual = CsvExportFormatter.FormatDecimalRaw(latest.ActualValue);
                latestProgress = latest.ProgressPercent.HasValue
                    ? latest.ProgressPercent.Value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty;
                latestStatus = latest.Status;
                reportedUtc = latest.ReportedAtUtc;
                latestNotes = latest.Notes ?? string.Empty;
            }

            lines.Add(
            [
                CsvExportFormatter.FormatGuid(mi.Id),
                mi.ActionItemId.HasValue ? CsvExportFormatter.FormatGuid(mi.ActionItemId.Value) : string.Empty,
                CsvExportFormatter.EscapeTextCell(mi.ActionTitle),
                CsvExportFormatter.EscapeTextCell(mi.Name),
                CsvExportFormatter.EscapeTextCell(mi.Description),
                CsvExportFormatter.FormatDecimalRaw(mi.BaselineValue),
                CsvExportFormatter.FormatDecimalRaw(mi.TargetValue),
                CsvExportFormatter.EscapeTextCell(mi.Unit),
                CsvExportFormatter.EscapeTextCell(mi.Status),
                CsvExportFormatter.EscapeTextCell(latestPeriodLabel),
                latestActual,
                latestProgress,
                CsvExportFormatter.EscapeTextCell(string.IsNullOrEmpty(latestStatus) ? null : latestStatus),
                CsvExportFormatter.FormatInstant(reportedUtc),
                CsvExportFormatter.EscapeTextCell(string.IsNullOrEmpty(latestNotes) ? null : latestNotes),
                CsvExportFormatter.FormatInstant(mi.CreatedAtUtc)
            ]);
        }

        var csv = CsvExportFormatter.Build(headers, lines);
        return PlanExportCsvResult.Ok(csv);
    }
}
