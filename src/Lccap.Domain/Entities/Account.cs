using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class Account : BaseEntity
{
    /// <remarks>varchar(200) per baseline.</remarks>
    public string Name { get; set; } = string.Empty;

    /// <remarks>varchar(100)</remarks>
    public string Region { get; set; } = string.Empty;

    /// <remarks>varchar(100)</remarks>
    public string Province { get; set; } = string.Empty;

    /// <remarks>varchar(150)</remarks>
    public string MunicipalityOrCity { get; set; } = string.Empty;

    /// <remarks>varchar(50)</remarks>
    public string LguType { get; set; } = string.Empty;

    /// <remarks>varchar(255)</remarks>
    public string ContactEmail { get; set; } = string.Empty;

    /// <remarks>varchar(50) nullable.</remarks>
    public string? ContactPhone { get; set; }

    /// <remarks>'Active', 'Inactive', or 'Suspended'.</remarks>
    public string Status { get; set; } = "Active";

    /// <remarks>jsonb NOT NULL DEFAULT '{}'</remarks>
    public JsonDocument SettingsJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    // Navigation ---------------------------------------------------------
    public ICollection<User> Users { get; } = new List<User>();

    public ICollection<TenantSetting> TenantSettings { get; } = new List<TenantSetting>();

    public ICollection<Plan> Plans { get; } = new List<Plan>();

    public ICollection<FileAsset> FileAssets { get; } = new List<FileAsset>();

    /// <remarks>Optional audit navigations targeting <see cref="User"/> rows.</remarks>
    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }

    /// <summary>
    /// Soft-delete bookkeeping (caller supplies clock context; interceptor may also stamp Deletes).
    /// </summary>
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
