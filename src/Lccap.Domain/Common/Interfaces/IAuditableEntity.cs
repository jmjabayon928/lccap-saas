namespace Lccap.Domain.Common.Interfaces;

/// <summary>
/// Maps to PostgreSQL <c>TIMESTAMPTZ</c> audit columns (<see cref="DateTimeOffset"/>).
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset? UpdatedAt { get; set; }

    Guid? CreatedByUserId { get; set; }

    Guid? UpdatedByUserId { get; set; }
}
