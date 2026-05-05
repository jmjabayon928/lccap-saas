using Lccap.Domain.Common.Interfaces;

namespace Lccap.Domain.Common.Entities;

public abstract class BaseEntity : IHasRowVersion
{
    /// <summary>Primary key mapped to PostgreSQL UUID.</summary>
    public Guid Id { get; set; }

    /// <summary>Mapped to BYTEA and configured as an optimistic concurrency token in persistence.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public void EnsureRowVersion()
    {
        if (RowVersion == null || RowVersion.Length == 0)
        {
            RotateRowVersion();
        }
    }

    public void RotateRowVersion()
    {
        var bytes = new byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        RowVersion = bytes;
    }
}
