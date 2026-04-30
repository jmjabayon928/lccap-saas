namespace Lccap.Domain.Common.Interfaces;

/// <summary>
/// Marks an entity scoped to a tenant. Future global filters can limit queries by tenant id.
/// </summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
