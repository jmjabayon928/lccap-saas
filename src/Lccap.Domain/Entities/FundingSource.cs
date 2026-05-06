using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class FundingSource : BaseEntity
{
    public Guid AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SourceType { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ContactName { get; set; }

    public string? ContactEmail { get; set; }

    public string? WebsiteUrl { get; set; }

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
