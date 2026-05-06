using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class HazardLayer : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Guid? MapAssetId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string HazardType { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string? Source { get; set; }

    public string? Description { get; set; }

    public Guid? GeometryId { get; set; }

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Plan Plan { get; set; } = null!;

    public MapAsset? MapAsset { get; set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Archive(Guid deletedByUserId, DateTimeOffset deletedAtUtc)
    {
        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = deletedAtUtc;
    }
}

