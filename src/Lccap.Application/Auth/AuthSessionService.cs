using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Auth;

/// <summary>
/// Core session management for refresh-token based auth.
/// Never stores raw refresh tokens; only SHA-256 hashes.
/// Supports rotation, family revocation on reuse/revoked detection, and logout.
/// </summary>
public sealed class AuthSessionService
{
    private readonly ILccapDbContext _dbContext;
    private readonly IClock _clock;
    private readonly RefreshTokenService _tokenService;

    public AuthSessionService(ILccapDbContext dbContext, IClock clock, RefreshTokenService tokenService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
    }

    /// <summary>
    /// Creates a new refresh token session for the given user.
    /// </summary>
    public async Task<AuthSessionResult> CreateSessionAsync(User user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.UtcNow;
        var rawToken = _tokenService.GenerateRawToken();
        var tokenHash = _tokenService.HashToken(rawToken);
        var familyId = Guid.NewGuid();
        var expiresAt = now.AddDays(7);

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            AccountId = user.AccountId,
            TokenHash = tokenHash,
            TokenFamilyId = familyId,
            IssuedAtUtc = now,
            ExpiresAtUtc = expiresAt,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = now
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        _ = await _dbContext.SaveChangesAsync(ct);

        return new AuthSessionResult(rawToken, expiresAt);
    }

    /// <summary>
    /// Refreshes a session: validates incoming token, rotates it (new token same family),
    /// revokes old, and returns new raw token + user.
    /// On revoked/reused/expired/invalid: revokes entire family and fails.
    /// </summary>
    public async Task<AuthRefreshResult> RefreshSessionAsync(string rawRefreshToken, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedAccessException("Missing refresh token.");
        }

        var now = _clock.UtcNow;
        var incomingHash = _tokenService.HashToken(rawRefreshToken);

        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash && !t.IsDeleted, ct);

        if (existing is null || !existing.IsActive(now))
        {
            if (existing is not null)
            {
                await RevokeFamilyAsync(existing.TokenFamilyId, ipAddress, "Reuse or revoked token detected", ct);
            }

            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Rotate: revoke old token (user activity validated at controller using concrete db)
        existing.Revoke(now, ipAddress, "Rotated", null);

        // Issue replacement in same family
        var newRaw = _tokenService.GenerateRawToken();
        var newHash = _tokenService.HashToken(newRaw);
        var newExpires = now.AddDays(7);

        var replacement = new RefreshToken
        {
            UserId = existing.UserId,
            AccountId = existing.AccountId,
            TokenHash = newHash,
            TokenFamilyId = existing.TokenFamilyId,
            IssuedAtUtc = now,
            ExpiresAtUtc = newExpires,
            CreatedByIp = ipAddress,
            UserAgent = userAgent,
            CreatedAtUtc = now
        };

        _dbContext.RefreshTokens.Add(replacement);

        // Link old -> new
        existing.ReplacedByTokenId = replacement.Id;
        existing.UpdatedAtUtc = now;

        _ = await _dbContext.SaveChangesAsync(ct);

        return new AuthRefreshResult(existing.UserId, existing.AccountId, newRaw, newExpires);
    }

    /// <summary>
    /// Revokes the current active refresh token if present. Idempotent.
    /// </summary>
    public async Task LogoutAsync(string? rawRefreshToken, string? ipAddress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var now = _clock.UtcNow;
        var hash = _tokenService.HashToken(rawRefreshToken);

        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && !t.IsDeleted && t.RevokedAtUtc == null, ct);

        if (token is not null)
        {
            token.Revoke(now, ipAddress, "Logout", null);
            _ = await _dbContext.SaveChangesAsync(ct);
        }
    }

    private async Task RevokeFamilyAsync(Guid familyId, string? ipAddress, string reason, CancellationToken ct)
    {
        var now = _clock.UtcNow;
        var activeInFamily = await _dbContext.RefreshTokens
            .Where(t => t.TokenFamilyId == familyId && !t.IsDeleted && t.RevokedAtUtc == null)
            .ToListAsync(ct);

        foreach (var t in activeInFamily)
        {
            t.Revoke(now, ipAddress, reason, null);
        }

        if (activeInFamily.Count > 0)
        {
            _ = await _dbContext.SaveChangesAsync(ct);
        }
    }
}

/// <summary>
/// Result of creating a new session (login).
/// </summary>
public sealed record AuthSessionResult(string RefreshToken, DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Result of successful refresh (user identifiers for controller to load full details and generate token).
/// </summary>
public sealed record AuthRefreshResult(Guid UserId, Guid? AccountId, string RefreshToken, DateTimeOffset ExpiresAtUtc);
