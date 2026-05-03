using System.Text.Json;
using Lccap.Application.Common.Interfaces;
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

    public virtual async Task<IReadOnlyList<DocumentListItem>> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return Array.Empty<DocumentListItem>();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var rows = await (
                from d in _dbContext.Documents.AsNoTracking()
                join f in _dbContext.FileAssets.AsNoTracking() on d.FileAssetId equals f.Id
                where d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted
                orderby d.CreatedAtUtc descending
                select new { d, f })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(
            r => new DocumentListItem(
                r.d.Id,
                r.d.PlanId,
                r.d.FileAssetId,
                r.d.Category,
                r.d.Title,
                r.d.Description,
                r.d.DocumentDate,
                r.d.SourceAgency,
                DocumentTagParsing.ParseTags(r.d.TagsJson),
                r.f.OriginalFileName,
                r.f.ContentType,
                r.f.FileSizeBytes,
                r.f.CreatedAtUtc,
                r.d.CreatedAtUtc));
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
    IReadOnlyList<string> Tags,
    string? OriginalFileName,
    string? ContentType,
    long SizeBytes,
    DateTimeOffset FileCreatedAtUtc,
    DateTimeOffset CreatedAtUtc);
