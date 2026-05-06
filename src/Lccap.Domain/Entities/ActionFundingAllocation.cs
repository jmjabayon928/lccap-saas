using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ActionFundingAllocation : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public Guid ActionItemId { get; set; }

    public ActionItem ActionItem { get; set; } = null!;

    public Guid FundingSourceId { get; set; }

    public FundingSource FundingSource { get; set; } = null!;

    public Guid? FundingProgramId { get; set; }

    public FundingProgram? FundingProgram { get; set; }

    public Guid? FundingApplicationId { get; set; }

    public Guid? ClimateExpenditureTagId { get; set; }

    public ClimateExpenditureTag? ClimateExpenditureTag { get; set; }

    public int FiscalYear { get; set; }

    public decimal AllocatedAmount { get; set; }

    public decimal? CommittedAmount { get; set; }

    public decimal? ReleasedAmount { get; set; }

    public decimal? SpentAmount { get; set; }

    public string CurrencyCode { get; set; } = "PHP";

    public string AllocationStatus { get; set; } = "Planned";

    public string? Notes { get; set; }

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
