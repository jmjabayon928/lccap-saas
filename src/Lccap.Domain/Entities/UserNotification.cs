using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class UserNotification : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid NotificationEventId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public Guid AccountId { get; set; }

    public bool IsDeleted { get; set; }

    public NotificationEvent? NotificationEvent { get; set; }

    public User? User { get; set; }

    public Account? Account { get; set; }
}

