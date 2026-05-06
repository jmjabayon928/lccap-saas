using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications.Commands;

public sealed record CreateNotificationEventRequest(
    string EventType,
    JsonDocument PayloadJson,
    IReadOnlyList<Guid> RecipientUserIds);

public sealed record CreateNotificationEventResult(
    int StatusCode,
    IReadOnlyList<string> Errors,
    Guid? EventId,
    int CreatedNotificationCount);

public sealed class CreateNotificationEventCommand
{
    private static readonly HashSet<string> AllowedEventTypes = new(StringComparer.Ordinal)
    {
        "SectionCommentCreated",
        "SectionCommentResolved",
        "SectionCommentReopened",
        "SectionCommentArchived",
        "MonitoringUpdateCreated",
        "ActionFundingAllocationCreated",
        "ActionFundingAllocationArchived",
        "GeoJsonLayerCreated",
        "MapAssetArchived",
        "ExportPackageGenerated",
        "PlanUpdated",
        "General"
    };

    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public CreateNotificationEventCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<CreateNotificationEventResult> Execute(CreateNotificationEventRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return new CreateNotificationEventResult(403, new[] { "Forbidden." }, null, 0);
        }

        var normalizedEventType = request.EventType?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEventType) || !AllowedEventTypes.Contains(normalizedEventType))
        {
            return new CreateNotificationEventResult(400, new[] { "Invalid event type." }, null, 0);
        }

        if (request.PayloadJson is null)
        {
            return new CreateNotificationEventResult(400, new[] { "Payload is required." }, null, 0);
        }

        var payloadRoot = request.PayloadJson.RootElement;
        if (payloadRoot.ValueKind != JsonValueKind.Object)
        {
            return new CreateNotificationEventResult(400, new[] { "Payload must be a JSON object." }, null, 0);
        }

        var rawPayload = payloadRoot.GetRawText();
        if (rawPayload.Length > 50_000)
        {
            return new CreateNotificationEventResult(400, new[] { "Payload is too large." }, null, 0);
        }

        var distinctRecipientUserIds = request.RecipientUserIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (distinctRecipientUserIds.Count is < 1 or > 50)
        {
            return new CreateNotificationEventResult(400, new[] { "Recipient count must be between 1 and 50." }, null, 0);
        }

        var accountId = _currentUser.AccountId.Value;
        var creatorUserId = _currentUser.UserId.Value;

        // Ensure all recipients are active, non-deleted users in the current tenant.
        var recipients = await _db.Users
            .Where(u => u.AccountId == accountId && !u.IsDeleted && u.Status == "Active" && distinctRecipientUserIds.Contains(u.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (recipients.Count != distinctRecipientUserIds.Count)
        {
            return new CreateNotificationEventResult(400, new[] { "One or more recipients are invalid." }, null, 0);
        }

        // Create the notification event (single row) and user notifications (one per recipient).
        var now = _clock.UtcNow;

        var payloadDoc = JsonDocument.Parse(rawPayload, new JsonDocumentOptions());
        var notificationEvent = new NotificationEvent
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            EventType = normalizedEventType,
            PayloadJson = payloadDoc,
            CreatedAtUtc = now,
            CreatedByUserId = creatorUserId,
            UpdatedByUserId = null,
            IsDeleted = false
        };
        notificationEvent.EnsureRowVersion();

        _db.NotificationEvents.Add(notificationEvent);

        foreach (var recipientId in distinctRecipientUserIds)
        {
            var un = new UserNotification
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                UserId = recipientId,
                NotificationEventId = notificationEvent.Id,
                IsRead = false,
                ReadAtUtc = null,
                CreatedAtUtc = now,
                IsDeleted = false
            };
            un.EnsureRowVersion();
            _db.UserNotifications.Add(un);
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateNotificationEventResult(201, Array.Empty<string>(), notificationEvent.Id, distinctRecipientUserIds.Count);
    }
}

