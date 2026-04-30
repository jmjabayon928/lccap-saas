using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid? AccountId { get; set; }

    public Account? Account { get; set; }

    public Guid? UserId { get; set; }

    public User? User { get; set; }

    /// <remarks>varchar(100)</remarks>
    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    /// <remarks>varchar(50)</remarks>
    public string Action { get; set; } = string.Empty;

    public JsonDocument? OldValuesJson { get; set; }

    public JsonDocument? NewValuesJson { get; set; }

    /// <remarks>jsonb NOT NULL DEFAULT '{}'</remarks>
    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    /// <remarks>varchar(80) nullable.</remarks>
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
