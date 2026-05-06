using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class SectionComment : BaseEntity
{
    public Guid AccountId { get; set; }

    public Guid PlanId { get; set; }

    public string SectionKey { get; set; } = string.Empty;

    public string CommentType { get; set; } = "General";

    public string CommentText { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public bool IsResolved { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public Plan? Plan { get; set; }
}

