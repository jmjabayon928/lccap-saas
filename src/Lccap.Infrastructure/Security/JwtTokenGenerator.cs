using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Lccap.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace Lccap.Infrastructure.Security;

public sealed class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var signingKey = _configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var expirationMinutes = int.TryParse(_configuration["Jwt:ExpirationMinutes"], out var value) ? value : 60;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var exp = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes).ToUnixTimeSeconds();

        var headerJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT"
        });
        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["sub"] = user.Id.ToString(),
            ["account_id"] = user.AccountId?.ToString() ?? string.Empty,
            ["email"] = user.Email ?? string.Empty,
            ["role"] = user.Role ?? string.Empty,
            ["iss"] = issuer,
            ["aud"] = audience,
            ["iat"] = now,
            ["exp"] = exp
        });

        var header = Base64UrlEncode(Encoding.UTF8.GetBytes(headerJson));
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var unsignedToken = $"{header}.{payload}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(unsignedToken));
        var signature = Base64UrlEncode(signatureBytes);

        return $"{unsignedToken}.{signature}";
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
