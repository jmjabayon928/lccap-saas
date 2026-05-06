using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Queries;

public sealed class GetPlanOperationalDashboardQuery
{
    private static readonly string[] ActivityEntityNames =
    [
        "Document",
        "ActionItem",
        "MonitoringIndicator",
        "MonitoringUpdate",
        "SectionComment",
        "ActionFundingAllocation",
        "ExportJob",
    ];

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanOperationalDashboardQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanOperationalDashboardResult> Execute(
        Guid planId,
        int recentActivityLimit = 15,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanOperationalDashboardResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanOperationalDashboardResult.Forbidden();
        }

        var limit = ClampRecentActivityLimit(recentActivityLimit);
        var accountId = _currentUserContext.AccountId.Value;

        var planHead = await _dbContext.Plans.AsNoTracking()
            .Where(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted && p.Status != "Archived")
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.StartYear,
                p.EndYear,
                p.Status,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (planHead is null)
        {
            return GetPlanOperationalDashboardResult.NotFound();
        }

        var generatedAtUtc = DateTimeOffset.UtcNow;

        var docBase = _dbContext.Documents.AsNoTracking()
            .Where(d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted);

        var evidenceTask = AggregateEvidenceAsync(docBase, cancellationToken);

        var actionBase = _dbContext.ActionItems.AsNoTracking()
            .Where(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted);

        var actionTask = AggregateActionsAsync(actionBase, cancellationToken);

        var indicatorBase = _dbContext.MonitoringIndicators.AsNoTracking()
            .Where(i => i.AccountId == accountId && i.PlanId == planId && !i.IsDeleted);

        var monitoringTask = AggregateMonitoringAsync(_dbContext, accountId, planId, indicatorBase, cancellationToken);

        var commentBase = _dbContext.SectionComments.AsNoTracking()
            .Where(c => c.AccountId == accountId && c.PlanId == planId && !c.IsDeleted);

        var reviewTask = AggregateReviewAsync(commentBase, cancellationToken);

        var allocBase = _dbContext.ActionFundingAllocations.AsNoTracking()
            .Where(x => x.AccountId == accountId && x.PlanId == planId && !x.IsDeleted);

        var fundingTask = AggregateFundingAsync(allocBase, cancellationToken);

        var activityTask = BuildActivityAsync(accountId, planId, limit, cancellationToken);

        await Task.WhenAll(evidenceTask, actionTask, monitoringTask, reviewTask, fundingTask, activityTask).ConfigureAwait(false);

        var evidence = await evidenceTask.ConfigureAwait(false);
        var actions = await actionTask.ConfigureAwait(false);
        var monitoring = await monitoringTask.ConfigureAwait(false);
        var review = await reviewTask.ConfigureAwait(false);
        var funding = await fundingTask.ConfigureAwait(false);
        var activity = await activityTask.ConfigureAwait(false);

        var export = BuildExportReadiness(evidence, actions, monitoring, funding, review);
        var suggestedNextSteps = BuildSuggestedNextSteps(evidence, actions, monitoring, funding, review, export);

        var dto = new PlanOperationalDashboardDto(
            planHead.Id,
            planHead.Title,
            planHead.StartYear,
            planHead.EndYear,
            planHead.Status,
            generatedAtUtc,
            evidence,
            actions,
            monitoring,
            review,
            funding,
            export with { SuggestedNextSteps = suggestedNextSteps },
            activity);

        return GetPlanOperationalDashboardResult.Success(dto);
    }

    private static int ClampRecentActivityLimit(int requested)
    {
        if (requested < 1)
        {
            return 1;
        }

        return requested > 50 ? 50 : requested;
    }

    private static async Task<EvidenceDashboardSummaryDto> AggregateEvidenceAsync(
        IQueryable<Document> docBase,
        CancellationToken cancellationToken)
    {
        var total = await docBase.CountAsync(cancellationToken).ConfigureAwait(false);
        var draft = await docBase.CountAsync(d => d.EvidenceStatus == "Draft", cancellationToken).ConfigureAwait(false);
        var internalCount = await docBase.CountAsync(d => d.EvidenceStatus == "Internal", cancellationToken).ConfigureAwait(false);
        var official = await docBase.CountAsync(d => d.EvidenceStatus == "Official", cancellationToken).ConfigureAwait(false);
        var publicCount = await docBase.CountAsync(d => d.EvidenceStatus == "Public", cancellationToken).ConfigureAwait(false);
        var linkedSection = await docBase.CountAsync(d => d.PlanSectionId != null, cancellationToken).ConfigureAwait(false);
        var linkedAction = await docBase.CountAsync(d => d.ActionItemId != null, cancellationToken).ConfigureAwait(false);

        return new EvidenceDashboardSummaryDto(
            total,
            official,
            publicCount,
            draft,
            internalCount,
            linkedSection,
            linkedAction);
    }

    private static async Task<ActionDashboardSummaryDto> AggregateActionsAsync(
        IQueryable<ActionItem> actionBase,
        CancellationToken cancellationToken)
    {
        var totals = await actionBase
            .GroupBy(a => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Planned = g.Count(a => a.Status == "Planned"),
                InProgress = g.Count(a => a.Status == "InProgress"),
                OnTrack = g.Count(a => a.Status == "OnTrack"),
                Delayed = g.Count(a => a.Status == "Delayed"),
                Completed = g.Count(a => a.Status == "Completed"),
                Cancelled = g.Count(a => a.Status == "Cancelled"),
                WithBudget = g.Count(a => a.BudgetAmount > 0m),
                WithFundingSource = g.Count(a => a.FundingSource != null && a.FundingSource.Trim().Length > 0),
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        totals ??= new
        {
            Total = 0,
            Planned = 0,
            InProgress = 0,
            OnTrack = 0,
            Delayed = 0,
            Completed = 0,
            Cancelled = 0,
            WithBudget = 0,
            WithFundingSource = 0,
        };

        var missingFunding = totals.Total - totals.WithFundingSource;
        if (missingFunding < 0)
        {
            missingFunding = 0;
        }

        return new ActionDashboardSummaryDto(
            totals.Total,
            totals.Planned,
            totals.InProgress,
            totals.OnTrack,
            totals.Delayed,
            totals.Completed,
            totals.Cancelled,
            totals.WithBudget,
            totals.WithFundingSource,
            missingFunding);
    }

    private static async Task<MonitoringDashboardSummaryDto> AggregateMonitoringAsync(
        ILccapDbContext dbContext,
        Guid accountId,
        Guid planId,
        IQueryable<MonitoringIndicator> indicatorBase,
        CancellationToken cancellationToken)
    {
        var indicators = await indicatorBase
            .GroupBy(i => 1)
            .Select(g => new
            {
                Total = g.Count(),
                NotStarted = g.Count(i => i.Status == "NotStarted"),
                InProgress = g.Count(i => i.Status == "InProgress"),
                OnTrack = g.Count(i => i.Status == "OnTrack"),
                Delayed = g.Count(i => i.Status == "Delayed"),
                Completed = g.Count(i => i.Status == "Completed"),
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        indicators ??= new
        {
            Total = 0,
            NotStarted = 0,
            InProgress = 0,
            OnTrack = 0,
            Delayed = 0,
            Completed = 0,
        };

        var indicatorIds = await indicatorBase.Select(i => i.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var idSet = indicatorIds.Count == 0 ? null : indicatorIds.ToHashSet();

        var updatesBase = dbContext.MonitoringUpdates.AsNoTracking()
            .Where(u => u.AccountId == accountId && !u.IsDeleted);

        int totalUpdates;
        int indicatorsWithUpdates;
        DateTimeOffset? latestUpdateAt;

        if (idSet is null || idSet.Count == 0)
        {
            totalUpdates = 0;
            indicatorsWithUpdates = 0;
            latestUpdateAt = null;
        }
        else
        {
            totalUpdates = await updatesBase
                .CountAsync(u => idSet.Contains(u.MonitoringIndicatorId), cancellationToken)
                .ConfigureAwait(false);

            indicatorsWithUpdates = await updatesBase
                .Where(u => idSet.Contains(u.MonitoringIndicatorId))
                .Select(u => u.MonitoringIndicatorId)
                .Distinct()
                .CountAsync(cancellationToken)
                .ConfigureAwait(false);

            latestUpdateAt = await updatesBase
                .Where(u => idSet.Contains(u.MonitoringIndicatorId))
                .MaxAsync(u => (DateTimeOffset?)u.ReportedAtUtc, cancellationToken)
                .ConfigureAwait(false);
        }

        return new MonitoringDashboardSummaryDto(
            indicators.Total,
            indicators.NotStarted,
            indicators.InProgress,
            indicators.OnTrack,
            indicators.Delayed,
            indicators.Completed,
            totalUpdates,
            indicatorsWithUpdates,
            latestUpdateAt);
    }

    private static async Task<ReviewDashboardSummaryDto> AggregateReviewAsync(
        IQueryable<SectionComment> commentBase,
        CancellationToken cancellationToken)
    {
        var agg = await commentBase
            .GroupBy(c => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Unresolved = g.Count(c => !c.IsResolved),
                Resolved = g.Count(c => c.IsResolved),
                DataGap = g.Count(c => c.CommentType == "DataGap"),
                Validation = g.Count(c => c.CommentType == "Validation"),
                RevisionRequest = g.Count(c => c.CommentType == "RevisionRequest"),
            })
            .SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        agg ??= new
        {
            Total = 0,
            Unresolved = 0,
            Resolved = 0,
            DataGap = 0,
            Validation = 0,
            RevisionRequest = 0,
        };

        return new ReviewDashboardSummaryDto(
            agg.Total,
            agg.Unresolved,
            agg.Resolved,
            agg.DataGap,
            agg.Validation,
            agg.RevisionRequest);
    }

    private static async Task<FundingDashboardSummaryDto> AggregateFundingAsync(
        IQueryable<ActionFundingAllocation> allocBase,
        CancellationToken cancellationToken)
    {
        var total = await allocBase.CountAsync(cancellationToken).ConfigureAwait(false);
        var tagged = await allocBase.CountAsync(a => a.ClimateExpenditureTagId != null, cancellationToken).ConfigureAwait(false);
        var untagged = total - tagged;
        if (untagged < 0)
        {
            untagged = 0;
        }

        var currencyRows = await allocBase
            .GroupBy(a => a.CurrencyCode)
            .Select(g => new FundingCurrencyTotalDto(g.Key.Trim(), g.Sum(a => a.AllocatedAmount)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        currencyRows.Sort((a, b) => string.Compare(a.CurrencyCode, b.CurrencyCode, StringComparison.Ordinal));

        return new FundingDashboardSummaryDto(total, tagged, untagged, currencyRows);
    }

    private static ExportReadinessDashboardSummaryDto BuildExportReadiness(
        EvidenceDashboardSummaryDto evidence,
        ActionDashboardSummaryDto actions,
        MonitoringDashboardSummaryDto monitoring,
        FundingDashboardSummaryDto funding,
        ReviewDashboardSummaryDto review)
    {
        return new ExportReadinessDashboardSummaryDto(
            evidence.OfficialEvidenceCount > 0,
            actions.TotalActions > 0,
            monitoring.TotalIndicators > 0,
            funding.TotalAllocations > 0,
            review.UnresolvedComments > 0,
            []);
    }

    private static IReadOnlyList<string> BuildSuggestedNextSteps(
        EvidenceDashboardSummaryDto evidence,
        ActionDashboardSummaryDto actions,
        MonitoringDashboardSummaryDto monitoring,
        FundingDashboardSummaryDto funding,
        ReviewDashboardSummaryDto review,
        ExportReadinessDashboardSummaryDto exportSeed)
    {
        _ = exportSeed;

        List<string> steps = [];

        if (review.UnresolvedComments > 0)
        {
            steps.Add("Resolve open review comments.");
        }

        if (evidence.OfficialEvidenceCount == 0)
        {
            steps.Add("Add official evidence documents.");
        }

        if (actions.TotalActions == 0)
        {
            steps.Add("Define action items for this plan.");
        }
        else if (funding.TotalAllocations == 0)
        {
            steps.Add("Add funding allocations for priority actions.");
        }

        if (actions.MissingFundingSourceCount > 0)
        {
            steps.Add("Record funding sources on actions that still need them.");
        }

        if (monitoring.TotalIndicators > 0 && monitoring.TotalMonitoringUpdates == 0)
        {
            steps.Add("Add monitoring updates for tracked indicators.");
        }

        if (actions.TotalActions > 0 && monitoring.TotalIndicators == 0)
        {
            steps.Add("Track progress with monitoring indicators.");
        }

        // De-duplicate while preserving order
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var unique = new List<string>();
        foreach (var s in steps)
        {
            if (seen.Add(s))
            {
                unique.Add(s);
            }
        }

        return unique.Count <= 6 ? unique : unique.Take(6).ToList();
    }

    private async Task<IReadOnlyList<PlanOperationalActivityItemDto>> BuildActivityAsync(
        Guid accountId,
        Guid planId,
        int limit,
        CancellationToken cancellationToken)
    {
        var planIdStr = planId.ToString();

        var documentIds = await _dbContext.Documents.AsNoTracking()
            .Where(d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var actionIds = await _dbContext.ActionItems.AsNoTracking()
            .Where(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var indicatorIds = await _dbContext.MonitoringIndicators.AsNoTracking()
            .Where(i => i.AccountId == accountId && i.PlanId == planId && !i.IsDeleted)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var indicatorIdSet = indicatorIds.Count == 0 ? null : indicatorIds.ToHashSet();

        List<Guid> monitoringUpdateIds = [];
        if (indicatorIdSet is not null && indicatorIdSet.Count > 0)
        {
            monitoringUpdateIds = await _dbContext.MonitoringUpdates.AsNoTracking()
                .Where(u => u.AccountId == accountId && !u.IsDeleted && indicatorIdSet.Contains(u.MonitoringIndicatorId))
                .Select(u => u.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        var commentIds = await _dbContext.SectionComments.AsNoTracking()
            .Where(c => c.AccountId == accountId && c.PlanId == planId && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var allocationIds = await _dbContext.ActionFundingAllocations.AsNoTracking()
            .Where(a => a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var exportJobIds = await _dbContext.ExportJobs.AsNoTracking()
            .Where(e => e.AccountId == accountId && e.PlanId == planId && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var docSet = documentIds.Count == 0 ? null : documentIds.ToHashSet();
        var actionSet = actionIds.Count == 0 ? null : actionIds.ToHashSet();
        var monitoringUpdateSet = monitoringUpdateIds.Count == 0 ? null : monitoringUpdateIds.ToHashSet();
        var commentSet = commentIds.Count == 0 ? null : commentIds.ToHashSet();
        var allocationSet = allocationIds.Count == 0 ? null : allocationIds.ToHashSet();
        var exportSet = exportJobIds.Count == 0 ? null : exportJobIds.ToHashSet();

        var buffer = Math.Min(2000, Math.Max(limit * 50, 200));

        var candidates = await _dbContext.AuditLogs.AsNoTracking()
            .Where(a => a.AccountId == accountId && ActivityEntityNames.Contains(a.EntityName))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(buffer)
            .Select(a => new ActivityCandidate(a.Id, a.Action, a.EntityName, a.EntityId, a.CreatedAtUtc, a.MetadataJson))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = new List<PlanOperationalActivityItemDto>(limit);
        foreach (var row in candidates)
        {
            if (!IsPlanScopedAudit(row, planIdStr, docSet, actionSet, indicatorIdSet, monitoringUpdateSet, commentSet, allocationSet, exportSet))
            {
                continue;
            }

            var summary = BuildActivitySummary(row.Action, row.EntityName);
            items.Add(new PlanOperationalActivityItemDto(row.Id, row.Action, row.EntityName, row.EntityId, row.CreatedAtUtc, summary));
            if (items.Count >= limit)
            {
                break;
            }
        }

        return items;
    }

    private static bool IsPlanScopedAudit(
        ActivityCandidate row,
        string planIdStr,
        HashSet<Guid>? documentIds,
        HashSet<Guid>? actionIds,
        HashSet<Guid>? indicatorIds,
        HashSet<Guid>? monitoringUpdateIds,
        HashSet<Guid>? commentIds,
        HashSet<Guid>? allocationIds,
        HashSet<Guid>? exportJobIds)
    {
        if (TryMatchMetadataPlanId(row.MetadataJson, planIdStr))
        {
            return true;
        }

        if (row.EntityId is null || row.EntityId.Value == Guid.Empty)
        {
            return false;
        }

        var id = row.EntityId.Value;
        return row.EntityName switch
        {
            "Document" => documentIds?.Contains(id) == true,
            "ActionItem" => actionIds?.Contains(id) == true,
            "MonitoringIndicator" => indicatorIds?.Contains(id) == true,
            "MonitoringUpdate" => monitoringUpdateIds?.Contains(id) == true,
            "SectionComment" => commentIds?.Contains(id) == true,
            "ActionFundingAllocation" => allocationIds?.Contains(id) == true,
            "ExportJob" => exportJobIds?.Contains(id) == true,
            _ => false,
        };
    }

    private static bool TryMatchMetadataPlanId(JsonDocument metadata, string planIdStr)
    {
        if (metadata.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!metadata.RootElement.TryGetProperty("planId", out var prop))
        {
            return false;
        }

        return prop.ValueKind == JsonValueKind.String && prop.GetString() == planIdStr;
    }

    private static string BuildActivitySummary(string action, string entityType)
    {
        var a = string.IsNullOrWhiteSpace(action) ? "Activity" : action.Trim();
        var e = string.IsNullOrWhiteSpace(entityType) ? "Entity" : entityType.Trim();
        return $"{a} · {e}";
    }

    private sealed record ActivityCandidate(
        Guid Id,
        string Action,
        string EntityName,
        Guid? EntityId,
        DateTimeOffset CreatedAtUtc,
        JsonDocument MetadataJson);
}

public sealed class GetPlanOperationalDashboardResult
{
    private GetPlanOperationalDashboardResult(bool isSuccess, int statusCode, PlanOperationalDashboardDto? dashboard)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Dashboard = dashboard;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public PlanOperationalDashboardDto? Dashboard { get; }

    public static GetPlanOperationalDashboardResult Success(PlanOperationalDashboardDto dashboard) => new(true, 200, dashboard);

    public static GetPlanOperationalDashboardResult Unauthorized() => new(false, 401, null);

    public static GetPlanOperationalDashboardResult Forbidden() => new(false, 403, null);

    public static GetPlanOperationalDashboardResult NotFound() => new(false, 404, null);
}

public sealed record PlanOperationalDashboardDto(
    Guid PlanId,
    string PlanTitle,
    int PlanningPeriodStart,
    int PlanningPeriodEnd,
    string Status,
    DateTimeOffset GeneratedAtUtc,
    EvidenceDashboardSummaryDto Evidence,
    ActionDashboardSummaryDto Actions,
    MonitoringDashboardSummaryDto Monitoring,
    ReviewDashboardSummaryDto Review,
    FundingDashboardSummaryDto Funding,
    ExportReadinessDashboardSummaryDto ExportReadiness,
    IReadOnlyList<PlanOperationalActivityItemDto> RecentActivity);

public sealed record EvidenceDashboardSummaryDto(
    int TotalDocuments,
    int OfficialEvidenceCount,
    int PublicEvidenceCount,
    int DraftEvidenceCount,
    int InternalEvidenceCount,
    int LinkedToSectionCount,
    int LinkedToActionCount);

public sealed record ActionDashboardSummaryDto(
    int TotalActions,
    int PlannedCount,
    int InProgressCount,
    int OnTrackCount,
    int DelayedCount,
    int CompletedCount,
    int CancelledCount,
    int ActionsWithBudgetCount,
    int ActionsWithFundingSourceCount,
    int MissingFundingSourceCount);

public sealed record MonitoringDashboardSummaryDto(
    int TotalIndicators,
    int NotStartedCount,
    int InProgressCount,
    int OnTrackCount,
    int DelayedCount,
    int CompletedCount,
    int TotalMonitoringUpdates,
    int IndicatorsWithUpdatesCount,
    DateTimeOffset? LatestMonitoringUpdateAtUtc);

public sealed record ReviewDashboardSummaryDto(
    int TotalComments,
    int UnresolvedComments,
    int ResolvedComments,
    int DataGapComments,
    int ValidationComments,
    int RevisionRequestComments);

public sealed record FundingDashboardSummaryDto(
    int TotalAllocations,
    int CcetTaggedAllocations,
    int UntaggedAllocations,
    IReadOnlyList<FundingCurrencyTotalDto> AllocationTotalsByCurrency);

public sealed record FundingCurrencyTotalDto(string CurrencyCode, decimal TotalAllocatedAmount);

public sealed record ExportReadinessDashboardSummaryDto(
    bool HasOfficialEvidence,
    bool HasActions,
    bool HasMonitoring,
    bool HasFundingAllocations,
    bool HasUnresolvedComments,
    IReadOnlyList<string> SuggestedNextSteps);

public sealed record PlanOperationalActivityItemDto(
    Guid Id,
    string Action,
    string EntityType,
    Guid? EntityId,
    DateTimeOffset CreatedAtUtc,
    string Summary);
