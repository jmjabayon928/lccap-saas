using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
using Lccap.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lccap.Infrastructure.Tests.Seed;

public class DemoSeedServiceTests
{
    private sealed class TestLccapDbContext : LccapDbContext
    {
        public TestLccapDbContext(DbContextOptions<LccapDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(System.Text.Json.JsonDocument))
                    {
                        property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<System.Text.Json.JsonDocument, string>(
                            v => v.RootElement.GetRawText(),
                            v => System.Text.Json.JsonDocument.Parse(v, default)));
                    }
                }
            }
        }
    }

    private static LccapDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new TestLccapDbContext(options);
    }

    [Fact]
    public async Task Seed_creates_platform_user_with_null_account_and_platform_scope()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);

        // Act
        await service.SeedAsync("Password123!");

        // Assert
        var platformUser = await ctx.Users.FirstOrDefaultAsync(u => u.UserScope == "Platform");
        Assert.NotNull(platformUser);
        Assert.Null(platformUser.AccountId);
        Assert.Equal("platform.admin@lccap.local", platformUser.Email);
        Assert.Equal("SystemAdmin", platformUser.Role);
        Assert.Equal("Active", platformUser.Status);
    }

    [Fact]
    public async Task Seed_creates_three_lgu_accounts()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);

        // Act
        await service.SeedAsync("Password123!");

        // Assert
        var accounts = await ctx.Accounts.ToListAsync();
        Assert.Equal(3, accounts.Count);
        Assert.Contains(accounts, a => a.Name == "Naga City Demo LGU");
        Assert.Contains(accounts, a => a.Name == "Pasig City Demo LGU");
        Assert.Contains(accounts, a => a.Name == "Quezon City Demo LGU");
    }

    [Fact]
    public async Task Seed_creates_planner_and_viewer_for_each_lgu()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);

        // Act
        await service.SeedAsync("Password123!");

        // Assert
        var accounts = await ctx.Accounts.ToListAsync();
        foreach (var account in accounts)
        {
            var users = await ctx.Users.Where(u => u.AccountId == account.Id).ToListAsync();
            Assert.Equal(2, users.Count);
            Assert.Contains(users, u => u.Role == "Planner");
            Assert.Contains(users, u => u.Role == "Viewer");
            Assert.All(users, u => Assert.Equal("Tenant", u.UserScope));
        }
    }

    [Fact]
    public async Task Seed_is_idempotent_when_run_twice()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);

        // Act
        await service.SeedAsync("Password123!");
        var userCount1 = await ctx.Users.CountAsync();
        var accountCount1 = await ctx.Accounts.CountAsync();

        await service.SeedAsync("Password123!");
        var userCount2 = await ctx.Users.CountAsync();
        var accountCount2 = await ctx.Accounts.CountAsync();

        // Assert
        Assert.Equal(userCount1, userCount2);
        Assert.Equal(accountCount1, accountCount2);
    }

    [Fact]
    public async Task Seed_uses_password_hasher_hash_that_verifies()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);
        var password = "Password123!";

        // Act
        await service.SeedAsync(password);

        // Assert
        var users = await ctx.Users.ToListAsync();
        Assert.All(users, u => Assert.True(hasher.Verify(password, u.PasswordHash)));
    }

    [Fact]
    public async Task Seed_does_not_require_roles_or_user_roles_for_current_login_model()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        using var ctx = CreateContext(dbName);
        var hasher = new PasswordHasher();
        var service = new DemoSeedService(ctx, hasher, NullLogger<DemoSeedService>.Instance);

        // Act
        await service.SeedAsync("Password123!");

        // Assert
        // The login model in AuthController only uses User.Email, User.PasswordHash, User.Status, User.IsDeleted, User.AccountId, User.Role.
        // We verify these are set and no other tables (Roles, UserRoles) are needed for the seed to be useful for login.
        var users = await ctx.Users.ToListAsync();
        Assert.All(users, u =>
        {
            Assert.False(string.IsNullOrWhiteSpace(u.Email));
            Assert.False(string.IsNullOrWhiteSpace(u.PasswordHash));
            Assert.Equal("Active", u.Status);
            Assert.False(u.IsDeleted);
            Assert.False(string.IsNullOrWhiteSpace(u.Role));
            // Platform users have null AccountId, Tenant users have non-null.
            if (u.UserScope == "Platform") Assert.Null(u.AccountId);
            else Assert.NotNull(u.AccountId);
        });
    }
}
