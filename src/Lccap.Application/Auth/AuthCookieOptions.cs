namespace Lccap.Application.Auth;

/// <summary>
/// Centralized cookie configuration for refresh token sessions.
/// Cookie is always HttpOnly + SameSite=Lax + Path=/api/auth.
/// Secure is true outside Development (controller decides based on env).
/// </summary>
public static class AuthCookieOptions
{
    public const string CookieName = "lccap_refresh_token";
    public const string Path = "/api/auth";
}
