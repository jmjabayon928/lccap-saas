using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class UserRole : BaseEntity
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    public string ScopeType { get; set; } = string.Empty;

    public string? ResourceType { get; set; }

    public Guid? ResourceId { get; set; }

    public DateTimeOffset AssignedAtUtc { get; set; }

    public DateTimeOffset? StartsAtUtc { get; set; }

    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public Guid? AssignedByUserId { get; set; }

    public User? AssignedByUser { get; set; }

    public string? AssignmentReason { get; set; }

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }
}
