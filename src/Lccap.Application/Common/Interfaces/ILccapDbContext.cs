using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Common.Interfaces;

public interface ILccapDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<PlanSection> PlanSections { get; }
    DbSet<SectionComment> SectionComments { get; }
    DbSet<ActionItem> ActionItems { get; }
    DbSet<MonitoringIndicator> MonitoringIndicators { get; }
    DbSet<MonitoringUpdate> MonitoringUpdates { get; }
    DbSet<FileAsset> FileAssets { get; }
    DbSet<Document> Documents { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<ExportJob> ExportJobs { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<ClimateExpenditureTag> ClimateExpenditureTags { get; }

    DbSet<FundingSource> FundingSources { get; }

    DbSet<FundingProgram> FundingPrograms { get; }

    DbSet<ActionFundingAllocation> ActionFundingAllocations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
