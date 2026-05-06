using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications.Commands;

public sealed record MarkAllNotificationsReadResult(
    int StatusCode,
    IReadOnlyList<string> Errors,
    int UpdatedCount);

public sealed class MarkAllNotificationsReadCommand
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public MarkAllNotificationsReadCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<MarkAllNotificationsReadResult> Execute(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return new MarkAllNotificationsReadResult(403, new[] { "Forbidden." }, 0);
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;
        var now = _clock.UtcNow;

        var unread = await _db.UserNotifications
            .Where(n => n.AccountId == accountId && n.UserId == userId && !n.IsDeleted && !n.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAtUtc = now;
            n.RotateRowVersion();
        }

        var updatedCount = unread.Count;
        if (updatedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new MarkAllNotificationsReadResult(200, Array.Empty<string>(), updatedCount);
    }
}

