using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
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

        var token = jwtTokenGenerator.GenerateToken(user);

        return Ok(new LoginResponse(
            token,
            user.Id,
            user.AccountId.Value,
            user.Email,
            user.FullName,
            user.Role));
    }

    public sealed record LoginRequest(string Email, string Password);

    public sealed record LoginResponse(
        string Token,
        Guid UserId,
        Guid AccountId,
        string Email,
        string FullName,
        string Role);
}
