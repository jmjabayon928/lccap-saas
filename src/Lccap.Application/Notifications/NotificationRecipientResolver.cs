using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications.Commands;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Notifications;

/// <summary>
/// Resolves in-tenant notification recipients and publishes workspace events without impacting the primary command transaction.
/// </summary>
public static class NotificationRecipientResolver
{
    private const string AdminRole = "Admin";
    private const string ReviewerRole = "Reviewer";

    private static readonly IClock UtcClockInstance = new UtcClock();

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class UtcClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    /// <summary>Returns up to 50 distinct user ids (Admin or Reviewer, active, same account), excluding <paramref name="excludeUserId"/> when set.</summary>
    public static async Task<IReadOnlyList<Guid>> ResolveAdminAndReviewerRecipientIdsAsync(
        ILccapDbContext db,
        Guid accountId,
        Guid? excludeUserId,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking()
            .Where(
                u => u.AccountId == accountId
                    && !u.IsDeleted
                    && u.Status == "Active"
                    && (u.Role == AdminRole || u.Role == ReviewerRole));

        if (excludeUserId.HasValue && excludeUserId.Value != Guid.Empty)
        {
            var excluded = excludeUserId.Value;
            query = query.Where(u => u.Id != excluded);
        }

        return await query
            .OrderBy(u => u.Id)
            .Select(u => u.Id)
            .Take(50)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Best-effort: never throws; skips when unauthenticated or no recipients; does not roll back caller work.</summary>
    public static async Task TryPublishWorkspaceEventAsync(
        ILccapDbContext db,
        ICurrentUserContext currentUser,
        IClock? clock,
        string eventType,
        string title,
        string message,
        string entityType,
        Guid entityId,
        Guid planId,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.AccountId is null || currentUser.UserId is null)
        {
            return;
        }

        try
        {
            var recipients = await ResolveAdminAndReviewerRecipientIdsAsync(
                    db,
                    currentUser.AccountId.Value,
                    currentUser.UserId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (recipients.Count == 0)
            {
                return;
            }

            using var payload = JsonSerializer.SerializeToDocument(
                new
                {
                    title,
                    message,
                    entityType,
                    entityId = entityId.ToString("D"),
                    planId = planId.ToString("D"),
                    source = "Workspace",
                },
                PayloadJsonOptions);

            var command = new CreateNotificationEventCommand(db, currentUser, clock ?? UtcClockInstance);
            _ = await command.Execute(
                    new CreateNotificationEventRequest(eventType, payload, recipients),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Primary operation remains committed; notification failure is non-fatal.
        }
    }
}
