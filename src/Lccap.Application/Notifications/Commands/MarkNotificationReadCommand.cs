using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications.Commands;

public sealed record MarkNotificationReadRequest(Guid NotificationId);

public sealed record MarkNotificationReadResult(
    int StatusCode,
    IReadOnlyList<string> Errors,
    bool IsRead,
    Guid NotificationId);

public sealed class MarkNotificationReadCommand
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public MarkNotificationReadCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<MarkNotificationReadResult> Execute(
        MarkNotificationReadRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return new MarkNotificationReadResult(403, new[] { "Forbidden." }, false, request.NotificationId);
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;

        var notification = await _db.UserNotifications
            .SingleOrDefaultAsync(
                n => n.Id == request.NotificationId
                    && n.AccountId == accountId
                    && n.UserId == userId
                    && !n.IsDeleted,
                cancellationToken);

        if (notification is null)
        {
            return new MarkNotificationReadResult(404, Array.Empty<string>(), false, request.NotificationId);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAtUtc = _clock.UtcNow;
            notification.RotateRowVersion();
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new MarkNotificationReadResult(200, Array.Empty<string>(), true, request.NotificationId);
    }
}

