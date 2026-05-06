using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Lccap.Api.Controllers;
using Lccap.Application.Auth;
using Lccap.Application.Common;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lccap.Api.Tests.Auth;

public sealed class AuthTests
{
    /// <summary>
    /// Simple IClock for tests (SystemClock may be internal).
    /// </summary>
    private sealed class TestClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    [Fact]
    public async Task ValidLogin_ReturnsToken()
    {
        var (controller, dbContext, _, _) = CreateAuthController();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var result = await controller.Login(
            new AuthController.LoginRequest(user.Email, "P@ssw0rd!"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));
    }

    [Fact]
    public async Task InvalidPassword_Returns401()
    {
        var (controller, dbContext, _, _) = CreateAuthController();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var result = await controller.Login(
            new AuthController.LoginRequest(user.Email, "wrong"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Theory]
    [InlineData("Suspended")]
    [InlineData("Inactive")]
    public async Task NonActiveUser_CannotLogin(string status)
    {
        var (controller, dbContext, _, _) = CreateAuthController();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, status);
        await dbContext.SaveChangesAsync();

        var result = await controller.Login(
            new AuthController.LoginRequest(user.Email, "P@ssw0rd!"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Token_IncludesAccountIdClaim()
    {
        var (controller, dbContext, _, _) = CreateAuthController();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var result = await controller.Login(
            new AuthController.LoginRequest(user.Email, "P@ssw0rd!"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        var accountIdClaim = GetJwtPayloadValue(payload.Token, "account_id");
        Assert.Equal(user.AccountId.ToString(), accountIdClaim);
    }

    [Fact]
    public void CurrentUserContext_ResolvesAccountId_FromClaims()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("sub", userId.ToString()),
                new Claim("account_id", accountId.ToString())
            },
            "Bearer"));

        var currentUser = new CurrentUserContext();
        currentUser.SetFromPrincipal(principal);

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal(accountId, currentUser.AccountId);
        Assert.Equal(userId, currentUser.UserId);
    }

    // --- Slice 2 refresh token session tests ---

    [Fact]
    public async Task Login_Sets_Refresh_Cookie_And_Returns_Access_Token()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var result = await controller.Login(
            new AuthController.LoginRequest(user.Email, "P@ssw0rd!"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));

        // Verify Set-Cookie header contains refresh cookie with HttpOnly
        Assert.True(controller.Response.Headers.TryGetValue("Set-Cookie", out var setCookie));
        var cookieHeader = setCookie.ToString();
        Assert.Contains(AuthCookieOptions.CookieName, cookieHeader);
        Assert.Contains("HttpOnly", cookieHeader, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_Stores_Only_Refresh_Token_Hash()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        await controller.Login(
            new AuthController.LoginRequest(user.Email, "P@ssw0rd!"),
            dbContext,
            passwordHasher,
            CreateTokenGenerator(),
            new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService()),
            CancellationToken.None);

        var stored = await dbContext.RefreshTokens.ToListAsync();
        Assert.Single(stored);
        var rt = stored[0];
        Assert.False(string.IsNullOrWhiteSpace(rt.TokenHash));
        Assert.Equal(64, rt.TokenHash.Length); // SHA256 hex
        // Raw token never stored
    }

    [Fact]
    public async Task Refresh_With_Valid_Cookie_Rotates_Refresh_Token_And_Returns_New_Access_Token()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var sessionService = new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService());
        var session = await sessionService.CreateSessionAsync(user, "127.0.0.1", "test-agent", CancellationToken.None);

        // Simulate cookie present
        controller.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [AuthCookieOptions.CookieName] = session.RefreshToken
        });

        var result = await controller.Refresh(
            dbContext,
            sessionService,
            CreateTokenGenerator(),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<AuthController.LoginResponse>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(payload.Token));

        // Old token should be revoked, new one created
        var tokens = await dbContext.RefreshTokens.Where(t => t.UserId == user.Id && !t.IsDeleted).ToListAsync();
        Assert.Equal(2, tokens.Count);
        var oldToken = tokens.SingleOrDefault(t => t.RevokedAtUtc != null);
        var newToken = tokens.SingleOrDefault(t => t.RevokedAtUtc == null);
        Assert.NotNull(oldToken);
        Assert.NotNull(newToken);
        Assert.Equal(oldToken!.ReplacedByTokenId, newToken!.Id);
    }

    [Fact]
    public async Task Refresh_With_Revoked_Token_Fails()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var sessionService = new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService());
        var session = await sessionService.CreateSessionAsync(user, null, null, CancellationToken.None);

        // Manually revoke the token
        var hash = new RefreshTokenService().HashToken(session.RefreshToken);
        var stored = await dbContext.RefreshTokens.FirstAsync(t => t.TokenHash == hash);
        stored.Revoke(DateTimeOffset.UtcNow, null, "test-revoke", null);
        await dbContext.SaveChangesAsync();

        controller.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [AuthCookieOptions.CookieName] = session.RefreshToken
        });

        var result = await controller.Refresh(dbContext, sessionService, CreateTokenGenerator(), CancellationToken.None);
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Logout_Revokes_Refresh_Token_And_Clears_Cookie()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        var sessionService = new AuthSessionService(dbContext, new TestClock(), new RefreshTokenService());
        var session = await sessionService.CreateSessionAsync(user, null, null, CancellationToken.None);

        controller.Request.Cookies = new TestCookieCollection(new Dictionary<string, string>
        {
            [AuthCookieOptions.CookieName] = session.RefreshToken
        });

        var result = await controller.Logout(sessionService, CancellationToken.None);
        Assert.IsType<NoContentResult>(result);

        var stored = await dbContext.RefreshTokens.FirstAsync(t => t.TokenHash == new RefreshTokenService().HashToken(session.RefreshToken));
        Assert.NotNull(stored.RevokedAtUtc);
        Assert.Equal("Logout", stored.RevokeReason);
    }

    [Fact]
    public async Task Me_Returns_Current_User_From_Access_Token()
    {
        var (controller, dbContext, _, _) = CreateAuthControllerWithHttpContext();
        var passwordHasher = new PasswordHasher();
        var user = SeedUser(dbContext, passwordHasher, "Active");
        await dbContext.SaveChangesAsync();

        // Simulate authenticated principal (from JWT)
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", user.Id.ToString()),
            new Claim("account_id", user.AccountId!.Value.ToString()),
            new Claim("email", user.Email),
            new Claim("role", user.Role)
        }, "Bearer"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        var result = await controller.Me(dbContext, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result);
        var me = Assert.IsType<AuthController.MeResponse>(ok.Value);
        Assert.Equal(user.Id, me.UserId);
        Assert.Equal(user.FullName, me.FullName);
    }

    private static (AuthController Controller, TestAuthDbContext DbContext, Guid AccountId, Guid UserId) CreateAuthController()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new TestAuthDbContext(options);
        return (new AuthController(), db, Guid.NewGuid(), Guid.NewGuid());
    }

    private static (AuthController Controller, TestAuthDbContext DbContext, Guid AccountId, Guid UserId) CreateAuthControllerWithHttpContext()
    {
        var (controller, db, accountId, userId) = CreateAuthController();
        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return (controller, db, accountId, userId);
    }

    /// <summary>
    /// Minimal IReadableStringCollection substitute for test cookie simulation.
    /// </summary>
    private sealed class TestCookieCollection : IRequestCookieCollection
    {
        private readonly IReadOnlyDictionary<string, string> _cookies;

        public TestCookieCollection(IReadOnlyDictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var v) ? v : null;

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys.ToList();

        public bool ContainsKey(string key) => _cookies.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();

        public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value!);
    }

    private static JwtTokenGenerator CreateTokenGenerator()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "test-issuer",
            ["Jwt:Audience"] = "test-audience",
            ["Jwt:SigningKey"] = "super-secret-signing-key-for-tests-12345",
            ["Jwt:ExpirationMinutes"] = "60"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new JwtTokenGenerator(configuration);
    }

    private static User SeedUser(TestAuthDbContext dbContext, PasswordHasher passwordHasher, string status)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = passwordHasher.Hash("P@ssw0rd!"),
            FullName = "Test User",
            Role = "Admin",
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        dbContext.Users.Add(user);
        return user;
    }

    private static string? GetJwtPayloadValue(string token, string claim)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        var bytes = Convert.FromBase64String(payload);
        var json = JsonDocument.Parse(bytes);
        return json.RootElement.TryGetProperty(claim, out var value) ? value.GetString() : null;
    }

    private sealed class TestAuthDbContext : LccapDbContext
    {
        public TestAuthDbContext(DbContextOptions<LccapDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Ignore<Account>();
            modelBuilder.Ignore<TenantSetting>();
            modelBuilder.Ignore<AuditLog>();
            modelBuilder.Ignore<Role>();
            modelBuilder.Ignore<Permission>();
            modelBuilder.Ignore<UserRole>();
            modelBuilder.Ignore<RolePermission>();
            modelBuilder.Ignore<Plan>();
            modelBuilder.Ignore<PlanSection>();
            modelBuilder.Ignore<FileAsset>();
            modelBuilder.Ignore<Document>();
            modelBuilder.Ignore<MonitoringIndicator>();
            modelBuilder.Ignore<MonitoringUpdate>();
            modelBuilder.Ignore<ActionItem>();
            modelBuilder.Ignore<ActionFundingAllocation>();
            modelBuilder.Ignore<ClimateExpenditureTag>();
            modelBuilder.Ignore<FundingProgram>();
            modelBuilder.Ignore<FundingSource>();
            modelBuilder.Ignore<Barangay>();
            modelBuilder.Ignore<CriticalFacility>();
            modelBuilder.Ignore<MapAsset>();
            modelBuilder.Ignore<MapAnnotation>();
            modelBuilder.Ignore<GeoJsonLayerFeature>();
            modelBuilder.Ignore<HazardLayer>();
            modelBuilder.Ignore<ExposureAnalysisJob>();
            modelBuilder.Ignore<NotificationEvent>();
            modelBuilder.Ignore<UserNotification>();
            modelBuilder.Ignore<NotificationTemplate>();
            modelBuilder.Ignore<CollaborationGroup>();
            modelBuilder.Ignore<CollaborationGroupMember>();
            // Do NOT ignore RefreshToken - it is required for Slice 2 session tests

            _ = modelBuilder.Entity<User>(builder =>
            {
                _ = builder.HasKey(x => x.Id);
                _ = builder.Property(x => x.AccountId).IsRequired();
                _ = builder.Property(x => x.Email).IsRequired();
                _ = builder.Property(x => x.PasswordHash).IsRequired();
                _ = builder.Property(x => x.FullName).IsRequired();
                _ = builder.Property(x => x.Role).IsRequired();
                _ = builder.Property(x => x.Status).IsRequired();
                _ = builder.Property(x => x.IsDeleted).IsRequired();
                _ = builder.Property(x => x.RowVersion).IsConcurrencyToken();
                builder.Ignore("Account");
                builder.Ignore("CreatedByUser");
                builder.Ignore("UpdatedByUser");
                builder.Ignore("DeletedByUser");
                builder.Ignore("CreatedPlans");
                builder.Ignore("CreatedPlanSections");
                builder.Ignore("CreatedDocuments");
                builder.Ignore("CreatedMonitoringIndicators");
            });

            _ = modelBuilder.Entity<RefreshToken>(builder =>
            {
                _ = builder.HasKey(x => x.Id);
                _ = builder.Property(x => x.UserId).IsRequired();
                _ = builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
                _ = builder.Property(x => x.TokenFamilyId).IsRequired();
                _ = builder.Property(x => x.IssuedAtUtc).IsRequired();
                _ = builder.Property(x => x.ExpiresAtUtc).IsRequired();
                _ = builder.Property(x => x.IsDeleted).IsRequired();
                _ = builder.Property(x => x.RowVersion).IsConcurrencyToken();
                builder.Ignore("User");
                builder.Ignore("Account");
                builder.Ignore("ReplacedByToken");
            });
        }
    }
}
