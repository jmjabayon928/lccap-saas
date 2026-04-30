using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Common.Interfaces;

public interface ILccapDbContext
{
    DbSet<Plan> Plans { get; }
    DbSet<PlanSection> PlanSections { get; }
    DbSet<ActionItem> ActionItems { get; }
    DbSet<FileAsset> FileAssets { get; }
    DbSet<Document> Documents { get; }
    DbSet<ExportJob> ExportJobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
