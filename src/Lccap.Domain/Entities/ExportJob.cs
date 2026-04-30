using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ExportJob : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public string ExportType { get; set; } = "Pdf";

    public string Status { get; set; } = "Queued";

    public Guid? FileAssetId { get; set; }

    public string? ErrorMessage { get; set; }

    public string OptionsJson { get; set; } = "{}";

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public void MarkRunning(DateTimeOffset startedAtUtc)
    {
        Status = "Running";
        StartedAtUtc = startedAtUtc;
        UpdatedAtUtc = startedAtUtc;
    }

    public void MarkCompleted(Guid fileAssetId, DateTimeOffset completedAtUtc)
    {
        FileAssetId = fileAssetId;
        Status = "Completed";
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string errorMessage)
    {
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Export failed." : errorMessage;
        Status = "Failed";
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
