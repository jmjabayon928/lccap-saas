using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? AccountId { get; set; }

    /// <remarks>varchar(128) — stores only hash, never raw token.</remarks>
    public string TokenHash { get; set; } = string.Empty;

    public Guid TokenFamilyId { get; set; }

    public DateTimeOffset IssuedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    /// <remarks>varchar(80)</remarks>
    public string? CreatedByIp { get; set; }

    /// <remarks>varchar(80)</remarks>
    public string? RevokedByIp { get; set; }

    public string? UserAgent { get; set; }

    /// <remarks>varchar(100)</remarks>
    public string? RevokeReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    // Navigation properties -------------------------------------------------
    public User? User { get; set; }

    public Account? Account { get; set; }

    public RefreshToken? ReplacedByToken { get; set; }

    /// <summary>
    /// Returns true only for tokens that are not soft-deleted, not revoked, and not yet expired.
    /// </summary>
    public bool IsActive(DateTimeOffset now)
        => !IsDeleted && RevokedAtUtc == null && ExpiresAtUtc > now;

    /// <summary>
    /// Marks the token as revoked (rotation/expiry use case). Idempotent if already revoked.
    /// Rotates RowVersion for optimistic concurrency.
    /// </summary>
    public void Revoke(DateTimeOffset now, string? ipAddress, string reason, Guid? replacedByTokenId)
    {
        if (RevokedAtUtc != null)
        {
            return;
        }

        RevokedAtUtc = now;
        RevokedByIp = ipAddress;
        RevokeReason = reason;
        ReplacedByTokenId = replacedByTokenId;
        UpdatedAtUtc = now;
        RotateRowVersion();
    }

    /// <inheritdoc cref="Account.SoftDelete" />
    public void SoftDelete(DateTimeOffset atUtc, Guid? deletedByUserId)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = atUtc;
        DeletedByUserId = deletedByUserId;
    }
}
