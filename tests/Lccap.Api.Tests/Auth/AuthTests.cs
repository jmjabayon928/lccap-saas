using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Lccap.Api.Controllers;
using Lccap.Application.Common;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Lccap.Api.Tests.Auth;

public sealed class AuthTests
{
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

    private static (AuthController Controller, TestAuthDbContext DbContext, Guid AccountId, Guid UserId) CreateAuthController()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new TestAuthDbContext(options);
        return (new AuthController(), db, Guid.NewGuid(), Guid.NewGuid());
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
        }
    }
}
