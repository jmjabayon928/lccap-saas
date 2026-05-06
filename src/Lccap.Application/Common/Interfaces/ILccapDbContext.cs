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

    DbSet<Barangay> Barangays { get; }

    DbSet<CriticalFacility> CriticalFacilities { get; }

    DbSet<MapAsset> MapAssets { get; }

    DbSet<MapAnnotation> MapAnnotations { get; }

    DbSet<GeoJsonLayerFeature> GeoJsonLayerFeatures { get; }

    DbSet<HazardLayer> HazardLayers { get; }

    DbSet<ExposureAnalysisJob> ExposureAnalysisJobs { get; }

    DbSet<User> Users { get; }

    DbSet<NotificationEvent> NotificationEvents { get; }

    DbSet<UserNotification> UserNotifications { get; }

    DbSet<NotificationTemplate> NotificationTemplates { get; }

    DbSet<CollaborationGroup> CollaborationGroups { get; }

    DbSet<CollaborationGroupMember> CollaborationGroupMembers { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
