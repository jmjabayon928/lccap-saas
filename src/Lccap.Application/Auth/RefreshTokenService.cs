using System.Security.Cryptography;
using System.Text;

namespace Lccap.Application.Auth;

/// <summary>
/// Handles generation of raw refresh tokens (high entropy) and SHA-256 hashing for storage.
/// Raw tokens are never persisted.
/// </summary>
public sealed class RefreshTokenService
{
    /// <summary>
    /// Generates a URL-safe Base64 refresh token with at least 32 bytes of entropy.
    /// </summary>
    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Computes SHA-256 hash of the raw token and returns lowercase hex (64 chars, fits varchar(128)).
    /// </summary>
    public string HashToken(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes(rawToken);
        var hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
