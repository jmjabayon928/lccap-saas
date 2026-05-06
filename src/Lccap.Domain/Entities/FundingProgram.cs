using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class FundingProgram : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid FundingSourceId { get; set; }

    public FundingSource FundingSource { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string? ProgramCode { get; set; }

    public string? Description { get; set; }

    public string? EligibleUses { get; set; }

    public string? ApplicationUrl { get; set; }

    public DateTimeOffset? OpensAtUtc { get; set; }

    public DateTimeOffset? ClosesAtUtc { get; set; }

    public decimal? MaxAwardAmount { get; set; }

    public string CurrencyCode { get; set; } = "PHP";

    public string Status { get; set; } = "Active";

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
