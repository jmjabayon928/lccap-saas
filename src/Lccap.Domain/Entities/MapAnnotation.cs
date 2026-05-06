using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class MapAnnotation : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid MapAssetId { get; set; }

    public MapAsset MapAsset { get; set; } = null!;

    public JsonDocument GeometryJson { get; set; } = JsonDocument.Parse("{}");

    public string? Note { get; set; }

    public JsonDocument StyleJson { get; set; } = JsonDocument.Parse("{}");

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
