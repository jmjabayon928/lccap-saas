using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class MapAsset : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public Guid FileAssetId { get; set; }

    public FileAsset FileAsset { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string MapType { get; set; } = string.Empty;

    public string MapFormat { get; set; } = string.Empty;

    public string? Description { get; set; }

    public JsonDocument? BoundsJson { get; set; }

    public JsonDocument DefaultStyleJson { get; set; } = JsonDocument.Parse("{}");

    public Guid? UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
