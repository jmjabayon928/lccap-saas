using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Queries;

public sealed class GetEvidenceIndexByPlanQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetEvidenceIndexByPlanQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetEvidenceIndexByPlanResult> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated || !_currentUserContext.AccountId.HasValue)
        {
            return GetEvidenceIndexByPlanResult.CreateUnauthenticated();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planExists = await _dbContext.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);

        if (!planExists)
        {
            return GetEvidenceIndexByPlanResult.CreateNotFound(planId);
        }

        var rows = await (
            from d in _dbContext.Documents.AsNoTracking()
            join f in _dbContext.FileAssets.AsNoTracking() on d.FileAssetId equals f.Id
            join s in _dbContext.PlanSections.AsNoTracking()
                on d.PlanSectionId equals s.Id into sections
            from s in sections.DefaultIfEmpty()
            join a in _dbContext.ActionItems.AsNoTracking()
                on d.ActionItemId equals a.Id into actions
            from a in actions.DefaultIfEmpty()
            where d.AccountId == accountId
                && d.PlanId == planId
                && !d.IsDeleted
                && f.AccountId == accountId
                && !f.IsDeleted
                && (s == null || (s.AccountId == accountId && s.PlanId == planId && !s.IsDeleted))
                && (a == null || (a.AccountId == accountId && a.PlanId == planId && !a.IsDeleted))
            select new EvidenceIndexRow
            {
                DocumentId = d.Id,
                Title = d.Title,
                Category = d.Category,
                EvidenceStatus = d.EvidenceStatus,
                SourceAgency = d.SourceAgency,
                DocumentDate = d.DocumentDate,
                Description = d.Description,
                TagsJson = d.TagsJson,
                PlanSectionId = d.PlanSectionId,
                PlanSectionKey = s != null ? s.SectionKey : null,
                PlanSectionTitle = s != null ? s.Title : null,
                ActionItemId = d.ActionItemId,
                ActionTitle = a != null ? a.Title : null,
                ActionType = a != null ? a.ActionType : null,
                ActionSector = a != null ? a.Sector : null,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSizeBytes = f.FileSizeBytes,
                Sha256Hash = f.Sha256Hash,
                UploadedByUserId = d.UploadedByUserId,
                CreatedAtUtc = d.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(
            r => new EvidenceIndexItem(
                r.DocumentId,
                r.Title,
                r.Category,
                r.EvidenceStatus,
                r.SourceAgency,
                r.DocumentDate,
                r.Description,
                DocumentTagParsing.ParseTags(r.TagsJson),
                r.PlanSectionId,
                r.PlanSectionKey,
                r.PlanSectionTitle,
                r.ActionItemId,
                r.ActionTitle,
                r.ActionType,
                r.ActionSector,
                r.OriginalFileName,
                r.ContentType,
                r.FileSizeBytes,
                r.Sha256Hash,
                r.UploadedByUserId,
                r.CreatedAtUtc));

        var sorted = items
            .OrderBy(i => EvidenceStatusSortKey(i.EvidenceStatus))
            .ThenBy(i => i.Category)
            .ThenBy(i => i.DocumentDate is null)
            .ThenByDescending(i => i.DocumentDate)
            .ThenByDescending(i => i.CreatedAtUtc)
            .ThenBy(i => i.Title ?? string.Empty)
            .ToArray();

        var countsByEvidenceStatus = sorted
            .GroupBy(x => x.EvidenceStatus)
            .OrderBy(g => EvidenceStatusSortKey(g.Key))
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var countsByCategory = sorted
            .GroupBy(x => x.Category)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var result = new EvidenceIndexResult(
            planId,
            DateTimeOffset.UtcNow,
            sorted,
            countsByEvidenceStatus,
            countsByCategory,
            sorted.Length);

        return GetEvidenceIndexByPlanResult.CreateSuccess(result);
    }

    private static int EvidenceStatusSortKey(string? status) =>
        status switch
        {
            "Draft" => 0,
            "Internal" => 1,
            "Official" => 2,
            "Public" => 3,
            _ => 100
        };

    private sealed class EvidenceIndexRow
    {
        public Guid DocumentId { get; init; }
        public string? Title { get; init; }
        public string Category { get; init; } = string.Empty;
        public string EvidenceStatus { get; init; } = "Internal";
        public string? SourceAgency { get; init; }
        public DateOnly? DocumentDate { get; init; }
        public string? Description { get; init; }
        public JsonDocument TagsJson { get; init; } = JsonDocument.Parse("[]");
        public Guid? PlanSectionId { get; init; }
        public string? PlanSectionKey { get; init; }
        public string? PlanSectionTitle { get; init; }
        public Guid? ActionItemId { get; init; }
        public string? ActionTitle { get; init; }
        public string? ActionType { get; init; }
        public string? ActionSector { get; init; }
        public string OriginalFileName { get; init; } = string.Empty;
        public string ContentType { get; init; } = string.Empty;
        public long FileSizeBytes { get; init; }
        public string? Sha256Hash { get; init; }
        public Guid? UploadedByUserId { get; init; }
        public DateTimeOffset CreatedAtUtc { get; init; }
    }
}

public sealed record EvidenceIndexResult(
    Guid PlanId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<EvidenceIndexItem> Items,
    IReadOnlyDictionary<string, int> CountsByEvidenceStatus,
    IReadOnlyDictionary<string, int> CountsByCategory,
    int TotalCount);

public sealed record EvidenceIndexItem(
    Guid DocumentId,
    string? Title,
    string Category,
    string EvidenceStatus,
    string? SourceAgency,
    DateOnly? DocumentDate,
    string? Description,
    IReadOnlyList<string> Tags,
    Guid? PlanSectionId,
    string? PlanSectionKey,
    string? PlanSectionTitle,
    Guid? ActionItemId,
    string? ActionTitle,
    string? ActionType,
    string? ActionSector,
    string? OriginalFileName,
    string? ContentType,
    long FileSizeBytes,
    string? Sha256Hash,
    Guid? UploadedByUserId,
    DateTimeOffset CreatedAtUtc);

public sealed record GetEvidenceIndexByPlanResult(
    bool Success,
    bool NotFound,
    bool UnauthenticatedAccount,
    EvidenceIndexResult? Result)
{
    public static GetEvidenceIndexByPlanResult CreateSuccess(EvidenceIndexResult result) =>
        new(true, false, false, result);

    public static GetEvidenceIndexByPlanResult CreateNotFound(Guid planId) =>
        new(false, true, false, null);

    public static GetEvidenceIndexByPlanResult CreateUnauthenticated() =>
        new(false, false, true, null);
}

