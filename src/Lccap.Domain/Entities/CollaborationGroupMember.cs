namespace Lccap.Domain.Entities;

using Lccap.Domain.Common.Entities;

public sealed class CollaborationGroupMember : BaseEntity
{
    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = "Member";

    public Guid? AccountId { get; set; }

    public bool IsDeleted { get; set; }

    public CollaborationGroup? Group { get; set; }

    public User? User { get; set; }

    public Account? Account { get; set; }
}

