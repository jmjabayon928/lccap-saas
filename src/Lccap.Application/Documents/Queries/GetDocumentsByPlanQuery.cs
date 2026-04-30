using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Documents.Queries;

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

        return await _dbContext.Documents
            .AsNoTracking()
            .Where(d => d.AccountId == accountId && d.PlanId == planId && !d.IsDeleted)
            .Select(d => new DocumentListItem(
                d.Id,
                d.PlanId,
                d.FileAssetId,
                d.Category,
                d.Title,
                d.Description,
                d.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

public sealed record DocumentListItem(
    Guid Id,
    Guid PlanId,
    Guid FileAssetId,
    string Category,
    string? Title,
    string? Description,
    DateTimeOffset CreatedAtUtc);
