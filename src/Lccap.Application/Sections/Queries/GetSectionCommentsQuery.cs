using Lccap.Application.Common.Interfaces;
using Lccap.Application.Sections.Commands;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Queries;

public sealed record GetSectionCommentsResult(
    bool Success,
    bool NotFound,
    bool ForbiddenAccess,
    IReadOnlyList<SectionCommentDto> Comments)
{
    public static GetSectionCommentsResult Forbidden() => new(false, false, true, Array.Empty<SectionCommentDto>());
    public static GetSectionCommentsResult Missing() => new(false, true, false, Array.Empty<SectionCommentDto>());
    public static GetSectionCommentsResult Ok(IReadOnlyList<SectionCommentDto> comments) => new(true, false, false, comments);
}

public class GetSectionCommentsQuery
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public GetSectionCommentsQuery(ILccapDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public virtual async Task<GetSectionCommentsResult> ExecuteAsync(
        Guid planId,
        string sectionKey,
        bool includeResolved = true,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null)
        {
            return GetSectionCommentsResult.Forbidden();
        }

        var accountId = _currentUser.AccountId.Value;
        var key = (sectionKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return GetSectionCommentsResult.Missing();
        }

        var planExists = await _db.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);
        if (!planExists)
        {
            return GetSectionCommentsResult.Missing();
        }

        var sectionExists = await _db.PlanSections
            .AsNoTracking()
            .AnyAsync(
                s => s.PlanId == planId
                    && s.AccountId == accountId
                    && !s.IsDeleted
                    && s.SectionKey == key,
                cancellationToken);
        if (!sectionExists)
        {
            return GetSectionCommentsResult.Missing();
        }

        var query = _db.SectionComments
            .AsNoTracking()
            .Where(c => c.AccountId == accountId && c.PlanId == planId && c.SectionKey == key && !c.IsDeleted);

        if (!includeResolved)
        {
            query = query.Where(c => !c.IsResolved);
        }

        var rows = await query
            .OrderBy(c => c.IsResolved)
            .ThenByDescending(c => c.CreatedAtUtc)
            .Select(c => new SectionCommentDto(
                c.Id,
                c.PlanId,
                c.SectionKey,
                c.CommentType,
                c.CommentText,
                c.CreatedByUserId,
                c.CreatedAtUtc,
                c.IsResolved,
                c.ResolvedAtUtc,
                c.ResolvedByUserId,
                c.UpdatedAtUtc,
                c.RowVersion))
            .ToListAsync(cancellationToken);

        return GetSectionCommentsResult.Ok(rows);
    }
}

