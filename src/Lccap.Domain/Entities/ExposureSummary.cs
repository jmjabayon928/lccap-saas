using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ExposureSummary : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Guid? ExposureAnalysisJobId { get; set; }

    public Guid? BarangayId { get; set; }

    public Guid? CriticalFacilityId { get; set; }

    public Guid? HazardLayerId { get; set; }

    public string HazardType { get; set; } = string.Empty;

    public string? Severity { get; set; }

    public decimal? ExposedAreaHectares { get; set; }

    public int ExposedFacilityCount { get; set; }

    public int? ExposedPopulation { get; set; }

    public decimal? RiskScore { get; set; }

    public JsonDocument SummaryJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Plan Plan { get; set; } = null!;

    public ExposureAnalysisJob? ExposureAnalysisJob { get; set; }

    public HazardLayer? HazardLayer { get; set; }

    public Barangay? Barangay { get; set; }

    public CriticalFacility? CriticalFacility { get; set; }

    public void Archive(Guid deletedByUserId, DateTimeOffset deletedAtUtc)
    {
        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = deletedAtUtc;
        UpdatedAtUtc = deletedAtUtc;
        UpdatedByUserId = deletedByUserId;
    }
}

