using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class MonitoringUpdate : BaseEntity
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

    public Guid MonitoringIndicatorId { get; set; }

    public string PeriodLabel { get; set; } = string.Empty;

    public decimal? ActualValue { get; set; }

    public decimal? ProgressPercent { get; set; }

    public string Status { get; set; } = "NotStarted";

    public string? Notes { get; set; }

    public DateTimeOffset ReportedAtUtc { get; set; }

    public Guid? ReportedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public MonitoringIndicator MonitoringIndicator { get; set; } = null!;

    public void UpdateProgress(
        string periodLabel,
        decimal? actualValue,
        decimal? progressPercent,
        string status,
        string? notes,
        DateTimeOffset reportedAtUtc,
        Guid? reportedByUserId,
        Guid? updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(periodLabel))
        {
            throw new ArgumentException("Period label must not be blank.", nameof(periodLabel));
        }

        if (progressPercent is < 0m or > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(progressPercent), "Progress percent must be between 0 and 100.");
        }

        if (!AllowedStatuses.Contains(status))
        {
            throw new ArgumentException("Update status is invalid.", nameof(status));
        }

        PeriodLabel = periodLabel.Trim();
        ActualValue = actualValue;
        ProgressPercent = progressPercent;
        Status = status;
        Notes = notes;
        ReportedAtUtc = reportedAtUtc;
        ReportedByUserId = reportedByUserId;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
    }
}
