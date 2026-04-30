using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class Plan : BaseEntity
{
    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public int StartYear { get; set; }

    public int EndYear { get; set; }

    public string Status { get; set; } = "Draft";

    public string TemplateMode { get; set; } = "New";

    public int VersionNumber { get; set; } = 1;

    public string? Description { get; set; }

    public DateTimeOffset? SubmittedAtUtc { get; set; }

    public DateTimeOffset? ApprovedAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public ICollection<PlanSection> Sections { get; } = new List<PlanSection>();

    public ICollection<Document> Documents { get; } = new List<Document>();

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }
}
