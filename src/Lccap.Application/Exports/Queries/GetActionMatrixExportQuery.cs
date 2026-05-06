using Lccap.Application.Common.Interfaces;
using Lccap.Application.Exports;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Exports.Queries;

public sealed class GetActionMatrixExportQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetActionMatrixExportQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
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

        var rows = await _dbContext.ActionItems
            .AsNoTracking()
            .Where(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted)
            .OrderBy(a => a.Sector)
            .ThenBy(a => a.ActionType)
            .ThenBy(a => a.Status)
            .ThenBy(a => a.PriorityScore == null ? 1 : 0)
            .ThenByDescending(a => a.PriorityScore)
            .ThenBy(a => a.Title)
            .Select(
                a => new
                {
                    a.Id,
                    a.Title,
                    a.ActionType,
                    a.Sector,
                    a.ResponsibleOffice,
                    a.BudgetAmount,
                    a.FundingSource,
                    a.TimelineStartUtc,
                    a.TimelineEndUtc,
                    a.Kpi,
                    a.PriorityScore,
                    a.Status,
                    a.CreatedAtUtc
                })
            .ToListAsync(cancellationToken);

        var headers = new[]
        {
            "ActionId",
            "Title",
            "ActionType",
            "Sector",
            "ResponsibleOffice",
            "BudgetAmount",
            "FundingSource",
            "TimelineStartUtc",
            "TimelineEndUtc",
            "KPI",
            "PriorityScore",
            "Status",
            "CreatedAtUtc"
        };

        var lines = new List<IReadOnlyList<string>>(rows.Count);
        foreach (var r in rows)
        {
            lines.Add(
            [
                CsvExportFormatter.FormatGuid(r.Id),
                CsvExportFormatter.EscapeTextCell(r.Title),
                CsvExportFormatter.EscapeTextCell(r.ActionType),
                CsvExportFormatter.EscapeTextCell(r.Sector),
                CsvExportFormatter.EscapeTextCell(r.ResponsibleOffice),
                CsvExportFormatter.FormatDecimal(r.BudgetAmount),
                CsvExportFormatter.EscapeTextCell(r.FundingSource),
                CsvExportFormatter.FormatInstant(r.TimelineStartUtc),
                CsvExportFormatter.FormatInstant(r.TimelineEndUtc),
                CsvExportFormatter.EscapeTextCell(r.Kpi),
                r.PriorityScore.HasValue
                    ? r.PriorityScore.Value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)
                    : string.Empty,
                CsvExportFormatter.EscapeTextCell(r.Status),
                CsvExportFormatter.FormatInstant(r.CreatedAtUtc)
            ]);
        }

        var csv = CsvExportFormatter.Build(headers, lines);
        return PlanExportCsvResult.Ok(csv);
    }
}

public sealed record PlanExportCsvResult(bool Success, bool NotFound, bool Unauthenticated, string? CsvBody)
{
    public static PlanExportCsvResult Ok(string csv) =>
        new(true, false, false, csv);

    public static PlanExportCsvResult MissingPlan() =>
        new(false, true, false, null);

    public static PlanExportCsvResult UnauthenticatedAccount() =>
        new(false, false, true, null);
}
