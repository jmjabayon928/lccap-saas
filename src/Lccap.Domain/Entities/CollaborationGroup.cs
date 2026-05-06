using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class CollaborationGroup : BaseEntity
{
    public Guid AccountId { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public ICollection<CollaborationGroupMember> Members { get; } = new List<CollaborationGroupMember>();

    public Account? Account { get; set; }
}

