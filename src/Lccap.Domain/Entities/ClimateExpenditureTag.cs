using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ClimateExpenditureTag : BaseEntity
{
    public Guid AccountId { get; set; }

    public string TagCode { get; set; } = string.Empty;

    public string TagName { get; set; } = string.Empty;

    public string TagCategory { get; set; } = string.Empty;

    public decimal? WeightPercent { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
