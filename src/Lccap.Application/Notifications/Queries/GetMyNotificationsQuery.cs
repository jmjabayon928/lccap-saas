using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications.Queries;

public sealed record GetMyNotificationsRequest(int Limit = 25, bool UnreadOnly = false);

public sealed record MyNotificationSummary(
    Guid Id,
    Guid NotificationEventId,
    string EventType,
    string Title,
    string Message,
    string? EntityType,
    string? EntityId,
    string? PlanId,
    bool IsRead,
    DateTimeOffset? ReadAtUtc,
    DateTimeOffset CreatedAtUtc);

public sealed record GetMyNotificationsResult(
    int StatusCode,
    IReadOnlyList<string> Errors,
    IReadOnlyList<MyNotificationSummary> Items,
    int UnreadCount,
    int TotalCount,
    int Limit,
    bool UnreadOnly);

public sealed class GetMyNotificationsQuery
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public GetMyNotificationsQuery(ILccapDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetMyNotificationsResult> Execute(GetMyNotificationsRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return new GetMyNotificationsResult(403, new[] { "Forbidden." }, Array.Empty<MyNotificationSummary>(), 0, 0,
                request.Limit, request.UnreadOnly);
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;
        var limit = request.Limit < 1 ? 1 : request.Limit > 100 ? 100 : request.Limit;
        var unreadOnly = request.UnreadOnly;

        var baseQuery =
            from un in _db.UserNotifications.AsNoTracking()
            join ev in _db.NotificationEvents.AsNoTracking() on un.NotificationEventId equals ev.Id
            where un.AccountId == accountId
                  && un.UserId == userId
                  && !un.IsDeleted
                  && !ev.IsDeleted
            select new
            {
                UserNotification = un,
                EventType = ev.EventType,
                PayloadJson = ev.PayloadJson
            };

        if (unreadOnly)
        {
            baseQuery = baseQuery.Where(x => !x.UserNotification.IsRead);
        }

        var totalsBaseQuery =
            from un in _db.UserNotifications.AsNoTracking()
            join ev in _db.NotificationEvents.AsNoTracking() on un.NotificationEventId equals ev.Id
            where un.AccountId == accountId
                  && un.UserId == userId
                  && !un.IsDeleted
                  && !ev.IsDeleted
            select new
            {
                UserNotification = un
            };

        var unreadCount = await totalsBaseQuery.CountAsync(x => !x.UserNotification.IsRead, cancellationToken);
        var totalCount = await totalsBaseQuery.CountAsync(cancellationToken);

        var ordered = baseQuery
            .OrderBy(x => x.UserNotification.IsRead)
            .ThenByDescending(x => x.UserNotification.CreatedAtUtc)
            .Take(limit);

        var rows = await ordered.ToListAsync(cancellationToken);

        var items = new List<MyNotificationSummary>(rows.Count);
        foreach (var row in rows)
        {
            var payload = row.PayloadJson.RootElement;
            string title = row.EventType;
            string message = "You have a new update.";
            string? entityType = null;
            string? entityId = null;
            string? planId = null;

            if (TryGetString(payload, "title", out var titleVal))
            {
                title = titleVal;
            }

            if (TryGetString(payload, "message", out var messageVal))
            {
                message = messageVal;
            }

            if (TryGetString(payload, "entityType", out var entityTypeVal))
            {
                entityType = entityTypeVal;
            }

            if (TryGetString(payload, "entityId", out var entityIdVal) && Guid.TryParse(entityIdVal, out _))
            {
                entityId = entityIdVal;
            }

            if (TryGetString(payload, "planId", out var planIdVal) && Guid.TryParse(planIdVal, out _))
            {
                planId = planIdVal;
            }

            items.Add(new MyNotificationSummary(
                row.UserNotification.Id,
                row.UserNotification.NotificationEventId,
                row.EventType,
                title,
                message,
                entityType,
                entityId,
                planId,
                row.UserNotification.IsRead,
                row.UserNotification.ReadAtUtc,
                row.UserNotification.CreatedAtUtc));
        }

        return new GetMyNotificationsResult(
            200,
            Array.Empty<string>(),
            items,
            unreadCount,
            totalCount,
            limit,
            unreadOnly);
    }

    private static bool TryGetString(JsonElement root, string propertyName, out string value)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            value = string.Empty;
            return false;
        }

        if (root.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            value = prop.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        value = string.Empty;
        return false;
    }
}

