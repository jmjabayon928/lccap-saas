using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Pagination;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Queries;

public static class DocumentTagParsing
{
    public static IReadOnlyList<string> ParseTags(JsonDocument tagsJson)
    {
        try
        {
            if (tagsJson.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<string>();
            }

            var list = new List<string>();
            foreach (var el in tagsJson.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = el.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        list.Add(s);
                    }
                }
            }

            return list;
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}

public class GetDocumentsByPlanQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetDocumentsByPlanQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<PagedResult<DocumentListItem>> ExecuteAsync(Guid planId, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return new PagedResult<DocumentListItem>(Array.Empty<DocumentListItem>(), 1, 25, 0);
        }

        var accountId = _currentUserContext.AccountId.Value;
        var (p, ps) = PaginationHelper.Normalize(page, pageSize);

        var baseQuery = from d in _dbContext.Documents.AsNoTracking()
                        join f in _dbContext.FileAssets.AsNoTracking() on d.FileAssetId equals f.Id
                        where d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted
                        select new { d, f };

        var totalCount = await baseQuery.CountAsync(cancellationToken);

        var rows = await baseQuery
            .OrderByDescending(x => x.d.CreatedAtUtc)
            .Skip((p - 1) * ps)
            .Take(ps)
            .ToListAsync(cancellationToken);

        var items = rows.ConvertAll(
            r => new DocumentListItem(
                r.d.Id,
                r.d.PlanId,
                r.d.FileAssetId,
                r.d.Category,
                r.d.Title,
                r.d.Description,
                r.d.DocumentDate,
                r.d.SourceAgency,
                r.d.PlanSectionId,
                r.d.ActionItemId,
                r.d.EvidenceStatus,
                DocumentTagParsing.ParseTags(r.d.TagsJson),
                r.f.OriginalFileName,
                r.f.ContentType,
                r.f.FileSizeBytes,
                r.f.CreatedAtUtc,
                r.d.CreatedAtUtc));

        return new PagedResult<DocumentListItem>(items, p, ps, totalCount);
    }
}

public sealed record DocumentListItem(
    Guid Id,
    Guid PlanId,
    Guid FileAssetId,
    string Category,
    string? Title,
    string? Description,
    DateOnly? DocumentDate,
    string? SourceAgency,
    Guid? PlanSectionId,
    Guid? ActionItemId,
    string EvidenceStatus,
    IReadOnlyList<string> Tags,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    DateTimeOffset FileCreatedAtUtc,
    DateTimeOffset CreatedAtUtc);
