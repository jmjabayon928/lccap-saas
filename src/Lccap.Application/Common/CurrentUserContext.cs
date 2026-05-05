using Lccap.Application.Common.Interfaces;
using System.Security.Claims;

namespace Lccap.Application.Common;

/// <summary>
/// Mutable default scoped context (unauthenticated) until ASP.NET middleware or tests assign values.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    public Guid? UserId { get; set; }

    public Guid? AccountId { get; set; }

    public string? Role { get; set; }

    public bool IsAuthenticated { get; set; }

    public void SetFromPrincipal(ClaimsPrincipal? principal)
    {
        IsAuthenticated = principal?.Identity?.IsAuthenticated == true;
        if (!IsAuthenticated)
        {
            UserId = null;
            AccountId = null;
            Role = null;
            return;
        }

        UserId = TryParseGuid(
            principal!.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value);
        AccountId = TryParseGuid(principal.FindFirst("account_id")?.Value);
        Role = (principal.FindFirst("role")?.Value ?? principal.FindFirst(ClaimTypes.Role)?.Value)?.Trim();
    }

    private static Guid? TryParseGuid(string? value)
    {
        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }
}
