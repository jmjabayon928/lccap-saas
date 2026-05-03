using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class ActionItem : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string Sector { get; set; } = string.Empty;

    public string? ResponsibleOffice { get; set; }

    public decimal BudgetAmount { get; set; }

    public string? FundingSource { get; set; }

    public DateTimeOffset? TimelineStartUtc { get; set; }

    public DateTimeOffset? TimelineEndUtc { get; set; }

    public string? Kpi { get; set; }

    public decimal? PriorityScore { get; set; }

    public string Status { get; set; } = "Planned";

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }

    public void UpdateDetails(
        string title,
        string? description,
        string actionType,
        string sector,
        string? responsibleOffice,
        decimal budgetAmount,
        string? fundingSource,
        DateTimeOffset? timelineStartUtc,
        DateTimeOffset? timelineEndUtc,
        string? kpi,
        decimal? priorityScore,
        string status,
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        Title = title.Trim();
        Description = description;
        ActionType = actionType;
        Sector = sector.Trim();
        ResponsibleOffice = responsibleOffice;
        BudgetAmount = budgetAmount;
        FundingSource = fundingSource;
        TimelineStartUtc = timelineStartUtc;
        TimelineEndUtc = timelineEndUtc;
        Kpi = kpi;
        PriorityScore = priorityScore;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Archive(DateTimeOffset archivedAtUtc, Guid deletedByUserId)
    {
        IsDeleted = true;
        DeletedAtUtc = archivedAtUtc;
        DeletedByUserId = deletedByUserId;
        UpdatedAtUtc = archivedAtUtc;
        UpdatedByUserId = deletedByUserId;
    }
}
