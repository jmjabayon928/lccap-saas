using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Exports.Queries;

public sealed class ExportPackageManifestDto
{
    public required Guid PlanId { get; init; }

    public required string PlanTitle { get; init; }

    public required int PlanningPeriodStart { get; init; }

    public required int PlanningPeriodEnd { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset GeneratedAtUtc { get; init; }

    public required ExportPackageCountsDto Counts { get; init; }

    public required ExportPackageReadinessDto Readiness { get; init; }

    public required ExportPackageDownloadsDto AvailableDownloads { get; init; }

    public required IReadOnlyList<string> Notes { get; init; }
}

public sealed class ExportPackageCountsDto
{
    public required int Documents { get; init; }

    public required int OfficialEvidence { get; init; }

    public required int PublicEvidence { get; init; }

    public required int Actions { get; init; }

    public required int MonitoringIndicators { get; init; }

    public required int MonitoringUpdates { get; init; }

    public required int UnresolvedSectionComments { get; init; }

    public required int FundingAllocations { get; init; }

    public required int CcetTaggedAllocations { get; init; }
}

public sealed class ExportPackageReadinessDto
{
    public required bool HasOfficialEvidence { get; init; }

    public required bool HasActions { get; init; }

    public required bool HasMonitoring { get; init; }

    public required bool HasFundingAllocations { get; init; }

    public required bool HasUnresolvedComments { get; init; }
}

public sealed class ExportPackageDownloadsDto
{
    public required string EvidenceIndexCsv { get; init; }

    public required string ActionMatrixCsv { get; init; }

    public required string MonitoringMatrixCsv { get; init; }

    public required string FundingReadinessCsv { get; init; }
}

public sealed class GetExportPackageManifestQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IClock _clock;

    public GetExportPackageManifestQuery(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IClock clock)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _clock = clock;
    }

    public async Task<GetExportPackageManifestResult> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.AccountId.HasValue)
        {
            return GetExportPackageManifestResult.UnauthenticatedAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var plan = await _dbContext.Plans
            .AsNoTracking()
            .Where(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted)
            .Select(p => new { p.Id, p.Title, p.StartYear, p.EndYear, p.Status })
            .SingleOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return GetExportPackageManifestResult.MissingPlan();
        }

        var documents = await _dbContext.Documents
            .AsNoTracking()
            .CountAsync(d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted, cancellationToken);

        var officialEvidence = await _dbContext.Documents
            .AsNoTracking()
            .CountAsync(
                d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted && d.EvidenceStatus == "Official",
                cancellationToken);

        var publicEvidence = await _dbContext.Documents
            .AsNoTracking()
            .CountAsync(
                d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted && d.EvidenceStatus == "Public",
                cancellationToken);

        var actions = await _dbContext.ActionItems
            .AsNoTracking()
            .CountAsync(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted, cancellationToken);

        var monitoringIndicators = await _dbContext.MonitoringIndicators
            .AsNoTracking()
            .CountAsync(mi => mi.AccountId == accountId && mi.PlanId == planId && !mi.IsDeleted, cancellationToken);

        var monitoringUpdates = await _dbContext.MonitoringUpdates
            .AsNoTracking()
            .Join(
                _dbContext.MonitoringIndicators.AsNoTracking(),
                u => u.MonitoringIndicatorId,
                i => i.Id,
                (u, i) => new { u, i })
            .CountAsync(
                x => x.u.AccountId == accountId
                    && !x.u.IsDeleted
                    && x.i.PlanId == planId
                    && x.i.AccountId == accountId
                    && !x.i.IsDeleted,
                cancellationToken);

        var unresolvedSectionComments = await _dbContext.SectionComments
            .AsNoTracking()
            .CountAsync(
                c => c.AccountId == accountId && c.PlanId == planId && !c.IsDeleted && !c.IsResolved,
                cancellationToken);

        var fundingAllocations = await _dbContext.ActionFundingAllocations
            .AsNoTracking()
            .CountAsync(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted, cancellationToken);

        var ccetTaggedAllocations = await _dbContext.ActionFundingAllocations
            .AsNoTracking()
            .CountAsync(
                a => a.AccountId == accountId
                    && a.PlanId == planId
                    && !a.IsDeleted
                    && a.ClimateExpenditureTagId != null,
                cancellationToken);

        var planKey = plan.Id.ToString("D");
        var manifest = new ExportPackageManifestDto
        {
            PlanId = plan.Id,
            PlanTitle = plan.Title,
            PlanningPeriodStart = plan.StartYear,
            PlanningPeriodEnd = plan.EndYear,
            Status = plan.Status,
            GeneratedAtUtc = _clock.UtcNow,
            Counts = new ExportPackageCountsDto
            {
                Documents = documents,
                OfficialEvidence = officialEvidence,
                PublicEvidence = publicEvidence,
                Actions = actions,
                MonitoringIndicators = monitoringIndicators,
                MonitoringUpdates = monitoringUpdates,
                UnresolvedSectionComments = unresolvedSectionComments,
                FundingAllocations = fundingAllocations,
                CcetTaggedAllocations = ccetTaggedAllocations
            },
            Readiness = new ExportPackageReadinessDto
            {
                HasOfficialEvidence = officialEvidence > 0,
                HasActions = actions > 0,
                HasMonitoring = monitoringIndicators > 0,
                HasFundingAllocations = fundingAllocations > 0,
                HasUnresolvedComments = unresolvedSectionComments > 0
            },
            AvailableDownloads = new ExportPackageDownloadsDto
            {
                EvidenceIndexCsv = $"/api/plans/{planKey}/documents/evidence-index.csv",
                ActionMatrixCsv = $"/api/plans/{planKey}/exports/action-matrix.csv",
                MonitoringMatrixCsv = $"/api/plans/{planKey}/exports/monitoring-matrix.csv",
                FundingReadinessCsv = $"/api/plans/{planKey}/exports/funding-readiness.csv"
            },
            Notes =
            [
                "This package supports internal LCCAP preparation and review. It is not an official submission portal."
            ]
        };

        return GetExportPackageManifestResult.Ok(manifest);
    }
}

public sealed record GetExportPackageManifestResult(
    bool Success,
    bool NotFound,
    bool Unauthenticated,
    ExportPackageManifestDto? Manifest)
{
    public static GetExportPackageManifestResult Ok(ExportPackageManifestDto manifest) =>
        new(true, false, false, manifest);

    public static GetExportPackageManifestResult MissingPlan() =>
        new(false, true, false, null);

    public static GetExportPackageManifestResult UnauthenticatedAccount() =>
        new(false, false, true, null);
}
