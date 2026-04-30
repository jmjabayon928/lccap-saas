using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class PlanSection : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public Plan Plan { get; set; } = null!;

    public string SectionKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public Guid? LastEditedByUserId { get; set; }

    public DateTimeOffset? LastEditedAtUtc { get; set; }

    public JsonDocument SectionMetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public User? LastEditedByUser { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }

    public void UpdateContent(string title, string content, Guid editedByUserId, DateTimeOffset editedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        Title = title.Trim();
        Content = content ?? string.Empty;
        LastEditedByUserId = editedByUserId;
        LastEditedAtUtc = editedAtUtc;
        UpdatedAtUtc = editedAtUtc;
        UpdatedByUserId = editedByUserId;
    }
}
