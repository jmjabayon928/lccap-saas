using Lccap.Domain.Common.Interfaces;

namespace Lccap.Domain.Common.Entities;

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}
