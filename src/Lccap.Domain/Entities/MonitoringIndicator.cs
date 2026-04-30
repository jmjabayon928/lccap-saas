using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class MonitoringIndicator : BaseEntity
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "NotStarted",
        "InProgress",
        "OnTrack",
        "Delayed",
        "Completed"
    };

    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Guid? ActionItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal? BaselineValue { get; set; }

    public decimal? TargetValue { get; set; }

    public string? Unit { get; set; }

    public string Status { get; set; } = "NotStarted";

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Plan Plan { get; set; } = null!;

    public ICollection<MonitoringUpdate> Updates { get; } = new List<MonitoringUpdate>();

    public void UpdateDefinition(
        string name,
        string? description,
        decimal? baselineValue,
        decimal? targetValue,
        string? unit,
        string status,
        Guid? updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Indicator name must not be blank.", nameof(name));
        }

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Indicator status is invalid.", nameof(status));
        }

        Name = name.Trim();
        Description = description;
        BaselineValue = baselineValue;
        TargetValue = targetValue;
        Unit = unit;
        Status = status;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static bool IsValidStatus(string status)
    {
        return AllowedStatuses.Contains(status);
    }
}
