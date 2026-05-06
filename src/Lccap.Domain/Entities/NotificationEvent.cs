using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class NotificationEvent : BaseEntity
{
    public Guid AccountId { get; set; }

    /// <summary>Mapped to varchar(100) with ck_event_type_not_empty check constraint.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Mapped to jsonb payload_json. Root is expected to be an object for Phase 2.</summary>
    public JsonDocument PayloadJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    // Navigations are optional for this slice, but help EF model clarity.
    public Account? Account { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public ICollection<UserNotification> UserNotifications { get; } = new List<UserNotification>();
}

