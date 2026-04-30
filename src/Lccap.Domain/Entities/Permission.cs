using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class Permission : BaseEntity
{
    public string PermissionKey { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string ModuleKey { get; set; } = string.Empty;

    public string ActionKey { get; set; } = string.Empty;

    public string ScopeType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemPermission { get; set; } = true;

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

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}
