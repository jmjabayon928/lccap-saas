using Lccap.Domain.Common.Interfaces;

namespace Lccap.Domain.Common.Entities;

public abstract class AuditableTenantEntity : TenantEntity, IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }
}
