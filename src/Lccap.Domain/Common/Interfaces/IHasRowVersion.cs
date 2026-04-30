namespace Lccap.Domain.Common.Interfaces;

/// <summary>
/// Marker for optimistic concurrency using a PostgreSQL <c>BYTEA</c> (<c>xmin</c>-backed or app-managed) column.
/// Intended to be paired with Fluent API concurrency token mapping.
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
