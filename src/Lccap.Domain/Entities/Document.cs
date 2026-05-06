using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class Document : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public Guid FileAssetId { get; set; }

    public FileAsset FileAsset { get; set; } = null!;

    public Guid? PlanSectionId { get; set; }

    public Guid? ActionItemId { get; set; }

    public string EvidenceStatus { get; set; } = "Internal";

    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DocumentDate { get; set; }

    public string? SourceAgency { get; set; }

    public JsonDocument TagsJson { get; set; } = JsonDocument.Parse("[]");

    public Guid? UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Account Account { get; set; } = null!;

    public User? UploadedByUser { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }

    public void UpdateMetadata(
        string category,
        string? title,
        string? description,
        DateOnly? documentDate,
        string? sourceAgency,
        JsonDocument tagsJson,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        Category = category;
        Title = title;
        Description = description;
        DocumentDate = documentDate;
        SourceAgency = sourceAgency;
        TagsJson.Dispose();
        TagsJson = tagsJson;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void UpdateMetadataWithEvidenceLinks(
        string category,
        string? title,
        string? description,
        DateOnly? documentDate,
        string? sourceAgency,
        JsonDocument tagsJson,
        Guid? planSectionId,
        Guid? actionItemId,
        string evidenceStatus,
        DateTimeOffset updatedAtUtc,
        Guid? updatedByUserId)
    {
        Category = category;
        Title = title;
        Description = description;
        DocumentDate = documentDate;
        SourceAgency = sourceAgency;
        PlanSectionId = planSectionId;
        ActionItemId = actionItemId;
        EvidenceStatus = evidenceStatus;
        TagsJson.Dispose();
        TagsJson = tagsJson;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }

    public void Archive(DateTimeOffset archivedAtUtc, Guid deletedByUserId, Guid? updatedByUserId)
    {
        IsDeleted = true;
        DeletedAtUtc = archivedAtUtc;
        DeletedByUserId = deletedByUserId;
        UpdatedAtUtc = archivedAtUtc;
        UpdatedByUserId = updatedByUserId;
    }
}
