using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications.Queries;

public sealed record CollaborationMemberSummary(
    Guid UserId,
    string FullName,
    string Email,
    string Role);

public sealed record CollaborationGroupSummary(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    int MemberCount,
    IReadOnlyList<CollaborationMemberSummary> Members);

public sealed record GetCollaborationSummaryResult(
    int StatusCode,
    IReadOnlyList<string> Errors,
    IReadOnlyList<CollaborationGroupSummary> Groups,
    int TotalGroups,
    int TotalMembers);

public sealed class GetCollaborationSummaryQuery
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public GetCollaborationSummaryQuery(ILccapDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetCollaborationSummaryResult> Execute(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null)
        {
            return new GetCollaborationSummaryResult(403, new[] { "Forbidden." }, Array.Empty<CollaborationGroupSummary>(), 0, 0);
        }

        var accountId = _currentUser.AccountId.Value;

        var rows = await (
            from g in _db.CollaborationGroups.AsNoTracking()
            join m in _db.CollaborationGroupMembers.AsNoTracking() on g.Id equals m.GroupId
            join u in _db.Users.AsNoTracking() on m.UserId equals u.Id
            where g.AccountId == accountId
                  && !g.IsDeleted
                  && !m.IsDeleted
                  && m.AccountId == accountId
                  && u.AccountId == accountId
                  && !u.IsDeleted
                  && u.Status == "Active"
            select new
            {
                GroupId = g.Id,
                GroupName = g.Name,
                g.CreatedAtUtc,
                MemberUserId = u.Id,
                u.FullName,
                u.Email,
                MemberRole = m.Role
            })
            .ToListAsync(cancellationToken);

        var grouped = rows
            .GroupBy(r => new { r.GroupId, r.GroupName, r.CreatedAtUtc })
            .ToList();

        var groups = new List<CollaborationGroupSummary>(grouped.Count);
        var totalMembers = 0;
        foreach (var g in grouped)
        {
            var members = g.Select(x => new CollaborationMemberSummary(x.MemberUserId, x.FullName, x.Email, x.MemberRole))
                .ToList();

            totalMembers += members.Count;
            groups.Add(new CollaborationGroupSummary(g.Key.GroupId, g.Key.GroupName, g.Key.CreatedAtUtc, members.Count, members));
        }

        return new GetCollaborationSummaryResult(200, Array.Empty<string>(), groups, groups.Count, totalMembers);
    }
}

