using Lccap.Application.Auth;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        [FromServices] LccapDbContext dbContext,
        [FromServices] PasswordHasher passwordHasher,
        [FromServices] JwtTokenGenerator jwtTokenGenerator,
        [FromServices] AuthSessionService authSessionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized("Invalid credentials.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                u => !u.IsDeleted
                    && u.Status == "Active"
                    && u.Email.ToLower() == normalizedEmail,
                cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized("Invalid credentials.");
        }

        if (user.AccountId is null)
        {
            return Unauthorized("Invalid credentials.");
        }

        user.LastLoginAtUtc = DateTimeOffset.UtcNow;
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        // Create refresh session and set HttpOnly cookie (Slice 2)
        var session = await authSessionService.CreateSessionAsync(
            user,
            GetClientIp(),
            GetUserAgent(),
            cancellationToken);

        AppendRefreshCookie(session.RefreshToken, session.ExpiresAtUtc);

        var token = jwtTokenGenerator.GenerateToken(user);

        return Ok(new LoginResponse(
            token,
            user.Id,
            user.AccountId.Value,
            user.Email,
            user.FullName,
            user.Role));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromServices] LccapDbContext dbContext,
        [FromServices] AuthSessionService authSessionService,
        [FromServices] JwtTokenGenerator jwtTokenGenerator,
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(AuthCookieOptions.CookieName, out var rawToken) || string.IsNullOrWhiteSpace(rawToken))
        {
            return Unauthorized();
        }

        try
        {
            var result = await authSessionService.RefreshSessionAsync(
                rawToken,
                GetClientIp(),
                GetUserAgent(),
                cancellationToken);

            AppendRefreshCookie(result.RefreshToken, result.ExpiresAtUtc);

            // Load user for token generation and response (service avoids direct Users dependency on interface)
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == result.UserId && !u.IsDeleted && u.Status == "Active", cancellationToken);
            if (user is null || user.AccountId is null)
            {
                ClearRefreshCookie();
                return Unauthorized();
            }

            var accessToken = jwtTokenGenerator.GenerateToken(user);

            return Ok(new LoginResponse(
                accessToken,
                user.Id,
                user.AccountId.Value,
                user.Email,
                user.FullName,
                user.Role));
        }
        catch (UnauthorizedAccessException)
        {
            ClearRefreshCookie();
            return Unauthorized();
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromServices] AuthSessionService authSessionService,
        CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(AuthCookieOptions.CookieName, out var rawToken))
        {
            await authSessionService.LogoutAsync(rawToken, GetClientIp(), cancellationToken);
        }

        ClearRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(
        [FromServices] LccapDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(sub, out var userId))
        {
            return Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted && u.Status == "Active", cancellationToken);

        if (user is null || user.AccountId is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse(
            user.Id,
            user.AccountId,
            user.Email,
            user.FullName,
            user.Role));
    }

    private string? GetClientIp()
        => HttpContext?.Connection?.RemoteIpAddress?.ToString();

    private string? GetUserAgent()
        => HttpContext?.Request?.Headers?.UserAgent.ToString();

    private bool IsDevelopment()
        => string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

    private void AppendRefreshCookie(string rawToken, DateTimeOffset expiresAtUtc)
    {
        if (Response is null) return; // tests without HttpContext
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !IsDevelopment(),
            Path = AuthCookieOptions.Path,
            Expires = expiresAtUtc.UtcDateTime
        };

        Response.Cookies.Append(AuthCookieOptions.CookieName, rawToken, options);
    }

    private void ClearRefreshCookie()
    {
        if (Response is null) return;
        var options = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = !IsDevelopment(),
            Path = AuthCookieOptions.Path,
            Expires = DateTime.UtcNow.AddDays(-1)
        };

        Response.Cookies.Delete(AuthCookieOptions.CookieName, options);
    }

    public sealed record LoginRequest(string Email, string Password);

    public sealed record LoginResponse(
        string Token,
        Guid UserId,
        Guid AccountId,
        string Email,
        string FullName,
        string Role);

    public sealed record MeResponse(
        Guid UserId,
        Guid? AccountId,
        string Email,
        string FullName,
        string Role);
}
