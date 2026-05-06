using Lccap.Application.Common.Interfaces;
using Lccap.Application.Exports;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Exports.Queries;

public sealed class GetFundingReadinessExportQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetFundingReadinessExportQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
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

        var rows = await (
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
                    && alloc.PlanId == planId
                    && !alloc.IsDeleted
                    && actionItem.AccountId == accountId
                    && actionItem.PlanId == planId
                    && !actionItem.IsDeleted
                    && fs.AccountId == accountId
                    && !fs.IsDeleted
                    && (prog == null || (prog.AccountId == accountId && !prog.IsDeleted))
                    && (ccet == null || (ccet.AccountId == accountId && !ccet.IsDeleted))
                orderby alloc.FiscalYear ascending, actionItem.Title ascending, fs.Name ascending, alloc.CreatedAtUtc descending
                select new
                {
                    alloc.Id,
                    alloc.ActionItemId,
                    ActionTitle = actionItem.Title,
                    FundingSourceId = fs.Id,
                    FundingSourceName = fs.Name,
                    FundingSourceType = fs.SourceType,
                    FundingProgramId = alloc.FundingProgramId,
                    FundingProgramName = prog != null ? prog.Name : null,
                    FundingProgramCode = prog != null ? prog.ProgramCode : null,
                    ClimateExpenditureTagId = alloc.ClimateExpenditureTagId,
                    CcetCode = ccet != null ? ccet.TagCode : null,
                    CcetName = ccet != null ? ccet.TagName : null,
                    CcetCategory = ccet != null ? ccet.TagCategory : null,
                    alloc.FiscalYear,
                    alloc.AllocatedAmount,
                    alloc.CurrencyCode,
                    alloc.AllocationStatus,
                    alloc.Notes,
                    alloc.CreatedAtUtc
                })
            .ToListAsync(cancellationToken);

        var headers = new[]
        {
            "AllocationId",
            "ActionItemId",
            "ActionTitle",
            "FundingSourceId",
            "FundingSourceName",
            "FundingSourceType",
            "FundingProgramId",
            "FundingProgramName",
            "FundingProgramCode",
            "ClimateExpenditureTagId",
            "CcetCode",
            "CcetName",
            "CcetCategory",
            "FiscalYear",
            "AllocatedAmount",
            "CurrencyCode",
            "AllocationStatus",
            "Notes",
            "CreatedAtUtc"
        };

        var lines = new List<IReadOnlyList<string>>(rows.Count);
        foreach (var r in rows)
        {
            lines.Add(
            [
                CsvExportFormatter.FormatGuid(r.Id),
                CsvExportFormatter.FormatGuid(r.ActionItemId),
                CsvExportFormatter.EscapeTextCell(r.ActionTitle),
                CsvExportFormatter.FormatGuid(r.FundingSourceId),
                CsvExportFormatter.EscapeTextCell(r.FundingSourceName),
                CsvExportFormatter.EscapeTextCell(r.FundingSourceType),
                r.FundingProgramId.HasValue ? CsvExportFormatter.FormatGuid(r.FundingProgramId.Value) : string.Empty,
                CsvExportFormatter.EscapeTextCell(r.FundingProgramName),
                CsvExportFormatter.EscapeTextCell(r.FundingProgramCode),
                r.ClimateExpenditureTagId.HasValue ? CsvExportFormatter.FormatGuid(r.ClimateExpenditureTagId.Value) : string.Empty,
                CsvExportFormatter.EscapeTextCell(r.CcetCode),
                CsvExportFormatter.EscapeTextCell(r.CcetName),
                CsvExportFormatter.EscapeTextCell(r.CcetCategory),
                r.FiscalYear.ToString(System.Globalization.CultureInfo.InvariantCulture),
                CsvExportFormatter.FormatDecimal(r.AllocatedAmount),
                CsvExportFormatter.EscapeTextCell(r.CurrencyCode?.Trim()),
                CsvExportFormatter.EscapeTextCell(r.AllocationStatus),
                CsvExportFormatter.EscapeTextCell(r.Notes),
                CsvExportFormatter.FormatInstant(r.CreatedAtUtc)
            ]);
        }

        var csv = CsvExportFormatter.Build(headers, lines);
        return PlanExportCsvResult.Ok(csv);
    }
}
