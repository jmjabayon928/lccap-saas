using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Queries;

public class GetPlanSectionHistoryQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanSectionHistoryQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<GetPlanSectionHistoryResult> ExecuteAsync(Guid planId, string sectionKey, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return GetPlanSectionHistoryResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var plan = await _dbContext.Plans
            .FirstOrDefaultAsync(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);

        if (plan == null)
        {
            return GetPlanSectionHistoryResult.Missing();
        }

        var section = await _dbContext.PlanSections
            .FirstOrDefaultAsync(s => s.PlanId == planId && s.SectionKey == sectionKey && s.AccountId == accountId && !s.IsDeleted, cancellationToken);

        if (section == null)
        {
            return GetPlanSectionHistoryResult.Missing();
        }

        var history = await _dbContext.AuditLogs
            .Where(a => a.AccountId == accountId
                && a.EntityName == "PlanSection"
                && a.EntityId == section.Id
                && (a.Action == "PlanSectionUpdated" || a.Action == "PlanSectionRestored"))
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new PlanSectionHistoryEntry
            {
                AuditLogId = a.Id,
                SectionId = section.Id,
                PlanId = planId,
                SectionKey = sectionKey,
                Action = a.Action,
                CreatedAtUtc = a.CreatedAtUtc,
                UserId = a.UserId,
                NewValuesJson = a.NewValuesJson
            })
            .ToListAsync(cancellationToken);

        var entries = history.Select(h =>
        {
            string title = string.Empty;
            string content = string.Empty;
            bool canRestore = false;

            if (h.NewValuesJson != null)
            {
                try
                {
                    var root = h.NewValuesJson.RootElement;
                    if (root.TryGetProperty("title", out var tProp)) title = tProp.GetString() ?? string.Empty;
                    if (root.TryGetProperty("content", out var cProp)) content = cProp.GetString() ?? string.Empty;
                    canRestore = !string.IsNullOrWhiteSpace(title);
                }
                catch
                {
                    // Skip malformed
                }
            }

            return new PlanSectionHistoryDto(
                h.AuditLogId,
                h.SectionId,
                h.PlanId,
                h.SectionKey,
                h.Action,
                title,
                content,
                h.CreatedAtUtc,
                h.UserId,
                canRestore);
        }).ToList();

        return GetPlanSectionHistoryResult.Ok(entries);
    }

    private class PlanSectionHistoryEntry
    {
        public Guid AuditLogId { get; set; }
        public Guid SectionId { get; set; }
        public Guid PlanId { get; set; }
        public string SectionKey { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTimeOffset CreatedAtUtc { get; set; }
        public Guid? UserId { get; set; }
        public JsonDocument? NewValuesJson { get; set; }
    }
}

public sealed record GetPlanSectionHistoryResult(
    bool Success,
    bool ForbiddenAccess,
    bool NotFound,
    List<PlanSectionHistoryDto> History,
    string? Error)
{
    public static GetPlanSectionHistoryResult Ok(List<PlanSectionHistoryDto> history) =>
        new(true, false, false, history, null);

    public static GetPlanSectionHistoryResult Missing() =>
        new(false, false, true, new(), null);

    public static GetPlanSectionHistoryResult Forbidden() =>
        new(false, true, false, new(), null);
}

public sealed record PlanSectionHistoryDto(
    Guid AuditLogId,
    Guid SectionId,
    Guid PlanId,
    string SectionKey,
    string Action,
    string Title,
    string Content,
    DateTimeOffset CreatedAtUtc,
    Guid? UserId,
    bool CanRestore);
