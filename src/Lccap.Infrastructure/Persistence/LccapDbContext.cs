using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Persistence;

public class LccapDbContext : DbContext, ILccapDbContext
{
    public LccapDbContext(DbContextOptions<LccapDbContext> options)
        : base(options)
    {
    }

    public DbSet<Account> Accounts => Set<Account>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TenantSetting> TenantSettings => Set<TenantSetting>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<PlanSection> PlanSections => Set<PlanSection>();

    public DbSet<SectionComment> SectionComments => Set<SectionComment>();

    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    public DbSet<FileAsset> FileAssets => Set<FileAsset>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<MonitoringIndicator> MonitoringIndicators => Set<MonitoringIndicator>();

    public DbSet<MonitoringUpdate> MonitoringUpdates => Set<MonitoringUpdate>();

    public DbSet<ClimateExpenditureTag> ClimateExpenditureTags => Set<ClimateExpenditureTag>();

    public DbSet<FundingSource> FundingSources => Set<FundingSource>();

    public DbSet<FundingProgram> FundingPrograms => Set<FundingProgram>();

    public DbSet<ActionFundingAllocation> ActionFundingAllocations => Set<ActionFundingAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Auditing is applied via SaveChanges interceptors registered in DI (see AuditSaveChangesInterceptor).
        // Additional global filters can be layered when tenant scoping is introduced.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LccapDbContext).Assembly);

    }
}
