using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class TenantSetting : BaseEntity
{
    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    /// <remarks>varchar(120)</remarks>
    public string SettingKey { get; set; } = string.Empty;

    /// <remarks>jsonb NOT NULL DEFAULT '{}'</remarks>
    public JsonDocument SettingValueJson { get; set; } = JsonDocument.Parse("{}");

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

    /// <inheritdoc cref="Account.SoftDelete" />
    public void SoftDelete(DateTimeOffset atUtc, Guid? deletedByUserId)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = atUtc;
        DeletedByUserId = deletedByUserId;
    }
}
