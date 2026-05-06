using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ExposureAnalysisJob : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public string Status { get; set; } = "Queued";

    public JsonDocument InputJson { get; set; } = JsonDocument.Parse("{}");

    public JsonDocument? OutputJson { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset? StartedAtUtc { get; set; }

    public DateTimeOffset? CompletedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Plan Plan { get; set; } = null!;

    public void MarkRunning(DateTimeOffset startedAtUtc)
    {
        MarkRunning(startedAtUtc, null);
    }

    public void MarkRunning(DateTimeOffset startedAtUtc, Guid? updatedByUserId)
    {
        Status = "Running";
        StartedAtUtc = startedAtUtc;
        UpdatedAtUtc = startedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void MarkCompleted(JsonDocument outputJson, DateTimeOffset completedAtUtc)
    {
        Status = "Completed";
        OutputJson = outputJson;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkFailed(string errorMessage, DateTimeOffset completedAtUtc)
    {
        MarkFailed(errorMessage, completedAtUtc, null);
    }

    public void MarkFailed(string errorMessage, DateTimeOffset completedAtUtc, Guid? updatedByUserId)
    {
        Status = "Failed";
        ErrorMessage = errorMessage.Trim();
        CompletedAtUtc = completedAtUtc;
        UpdatedAtUtc = completedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Archive(Guid deletedByUserId, DateTimeOffset deletedAtUtc)
    {
        IsDeleted = true;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = deletedAtUtc;
        UpdatedAtUtc = deletedAtUtc;
        UpdatedByUserId = deletedByUserId;
    }
}

