using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Lccap.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lccap.Infrastructure.Seed;

public sealed class DemoSeedService
{
    private readonly LccapDbContext _dbContext;
    private readonly PasswordHasher _passwordHasher;
    private readonly ILogger<DemoSeedService> _logger;

    public DemoSeedService(
        LccapDbContext dbContext,
        PasswordHasher passwordHasher,
        ILogger<DemoSeedService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(string password, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting demo data seeding...");

        var passwordHash = _passwordHasher.Hash(password);

        // 1. Seed Platform Admin
        await SeedPlatformUserAsync("platform.admin@lccap.local", "Platform Admin Demo", "SystemAdmin", passwordHash, cancellationToken);

        // 2. Seed LGU Accounts
        var nagaAccount = await SeedAccountAsync(
            "Naga City Demo LGU",
            "Region V - Bicol Region",
            "Camarines Sur",
            "Naga City",
            "City",
            "naga.demo@lccap.local",
            cancellationToken);

        var pasigAccount = await SeedAccountAsync(
            "Pasig City Demo LGU",
            "National Capital Region",
            "Metro Manila",
            "Pasig City",
            "City",
            "pasig.demo@lccap.local",
            cancellationToken);

        var quezonAccount = await SeedAccountAsync(
            "Quezon City Demo LGU",
            "National Capital Region",
            "Metro Manila",
            "Quezon City",
            "City",
            "quezon.demo@lccap.local",
            cancellationToken);

        // 3. Seed LGU Users
        await SeedTenantUserAsync(nagaAccount.Id, "naga.planner@lccap.local", "Naga Planner Demo", "Planner", passwordHash, cancellationToken);
        await SeedTenantUserAsync(nagaAccount.Id, "naga.viewer@lccap.local", "Naga Viewer Demo", "Viewer", passwordHash, cancellationToken);

        await SeedTenantUserAsync(pasigAccount.Id, "pasig.planner@lccap.local", "Pasig Planner Demo", "Planner", passwordHash, cancellationToken);
        await SeedTenantUserAsync(pasigAccount.Id, "pasig.viewer@lccap.local", "Pasig Viewer Demo", "Viewer", passwordHash, cancellationToken);

        await SeedTenantUserAsync(quezonAccount.Id, "quezon.planner@lccap.local", "Quezon Planner Demo", "Planner", passwordHash, cancellationToken);
        await SeedTenantUserAsync(quezonAccount.Id, "quezon.viewer@lccap.local", "Quezon Viewer Demo", "Viewer", passwordHash, cancellationToken);

        _logger.LogInformation("Demo data seeding completed.");
    }

    private async Task SeedPlatformUserAsync(string email, string fullName, string role, string passwordHash, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AccountId == null && u.Email.ToLower() == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (existingUser == null)
        {
            var user = new User
            {
                Email = email,
                FullName = fullName,
                Role = role,
                PasswordHash = passwordHash,
                UserScope = "Platform",
                AccountId = null,
                Status = "Active",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created platform user: {Email}", email);
        }
    }

    private async Task<Account> SeedAccountAsync(
        string name,
        string region,
        string province,
        string municipalityOrCity,
        string lguType,
        string contactEmail,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = contactEmail.ToLowerInvariant();
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.ContactEmail.ToLower() == normalizedEmail && !a.IsDeleted, cancellationToken);

        if (account == null)
        {
            account = new Account
            {
                Name = name,
                Region = region,
                Province = province,
                MunicipalityOrCity = municipalityOrCity,
                LguType = lguType,
                ContactEmail = contactEmail,
                Status = "Active",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.Accounts.Add(account);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created account: {Name}", name);
        }

        return account;
    }

    private async Task SeedTenantUserAsync(Guid accountId, string email, string fullName, string role, string passwordHash, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.ToLowerInvariant();
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.AccountId == accountId && u.Email.ToLower() == normalizedEmail && !u.IsDeleted, cancellationToken);

        if (existingUser == null)
        {
            var user = new User
            {
                AccountId = accountId,
                Email = email,
                FullName = fullName,
                Role = role,
                PasswordHash = passwordHash,
                UserScope = "Tenant",
                Status = "Active",
                CreatedAtUtc = DateTimeOffset.UtcNow
            };
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created tenant user: {Email} for account {AccountId}", email, accountId);
        }
    }
}
