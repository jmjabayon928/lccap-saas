namespace Lccap.Domain.Common.Interfaces;

/// <summary>
/// Null <see cref="DeletedAt"/> means not deleted — supports future global filters excluding soft-deleted rows.
/// </summary>
public interface ISoftDeleteEntity
{
    DateTimeOffset? DeletedAt { get; set; }
}
