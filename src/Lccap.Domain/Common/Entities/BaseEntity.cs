using Lccap.Domain.Common.Interfaces;

namespace Lccap.Domain.Common.Entities;

public abstract class BaseEntity : IHasRowVersion
{
    /// <summary>Primary key mapped to PostgreSQL UUID.</summary>
    public Guid Id { get; set; }

    /// <summary>Mapped to BYTEA and configured as an optimistic concurrency token in persistence.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
