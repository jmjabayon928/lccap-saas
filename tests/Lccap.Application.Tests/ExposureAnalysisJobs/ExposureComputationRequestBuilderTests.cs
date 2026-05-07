using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

namespace Lccap.Application.Tests.ExposureAnalysisJobs;

public sealed class ExposureComputationRequestBuilderTests
{
    [Fact]
    public async Task Build_request_succeeds_with_valid_stored_data()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId);
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}"",""requestedAtUtc"":""{DateTimeOffset.UtcNow:O}"",""requestedByUserId"":""{Guid.NewGuid()}"",""mode"":""BaselineExposure""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Request);
        Assert.Empty(result.ValidationErrors);

        var request = result.Request!;

        Assert.NotNull(request.CrsPolicy);
        Assert.Equal("Explicit", request.CrsPolicy!.SourceType);
        Assert.Equal(4326, request.CrsPolicy.SourceEpsg);
        Assert.True(request.CrsPolicy.FailOnAmbiguity);

        Assert.NotNull(request.GeometryPolicy);
        Assert.True(request.GeometryPolicy!.FailOnInvalidGeoJson);
        Assert.Equal("None", request.GeometryPolicy.RepairStrategy);

        Assert.NotNull(request.HazardLayer);
        Assert.Equal(hazardLayerId, request.HazardLayer!.Id);

        Assert.NotNull(request.HazardFeatures);
        Assert.Equal("FeatureCollection", request.HazardFeatures.RootElement.GetProperty("type").GetString());

        Assert.NotNull(request.Barangays);
        Assert.NotEmpty(request.Barangays);

        Assert.NotNull(request.CriticalFacilities);
        Assert.NotEmpty(request.CriticalFacilities);

        var validationErrors = ExposureComputationPayloadValidation.ValidateRequest(request);
        Assert.Empty(validationErrors);

        Assert.Equal(0, await db.ExposureSummaries.CountAsync());
    }

    [Fact]
    public async Task Build_request_fails_when_hazard_layer_id_missing()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse("{}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
        Assert.Equal("HazardLayerId is required and must be a valid GUID.", result.ErrorMessage);
    }

    [Fact]
    public async Task Build_request_fails_when_hazard_layer_missing_or_cross_tenant()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, Guid.NewGuid(), Guid.NewGuid(), hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId);
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Build_request_fails_when_hazard_layer_has_no_map_asset()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId: null, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId: Guid.NewGuid());
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Build_request_fails_when_no_hazard_features_exist()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Build_request_fails_when_no_barangay_boundaries_exist()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId);
        SeedBarangayWithoutBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Build_request_fails_when_no_facility_coordinates_exist()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId);
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: false);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ExposureComputationEngineErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task Build_request_does_not_create_exposure_summaries()
    {
        await using var db = CreateDbContext();
        var accountId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var hazardLayerId = Guid.NewGuid();
        var mapAssetId = Guid.NewGuid();

        SeedHazardLayer(db, accountId, planId, hazardLayerId, mapAssetId, isActive: true, isDeleted: false);
        SeedGeoJsonLayerFeature(db, accountId, mapAssetId);
        SeedBarangayWithBoundaryGeoJson(db, accountId);
        SeedCriticalFacility(db, accountId, planId, withCoordinates: true);

        var job = new ExposureAnalysisJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = JsonDocument.Parse($@"{{""hazardLayerId"":""{hazardLayerId}""}}"),
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        };

        var builder = new ExposureComputationRequestBuilder(db);
        var result = await builder.BuildAsync(job, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(await db.ExposureSummaries.ToListAsync());
    }

    private static TestLccapDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TestLccapDbContext>()
            .UseInMemoryDatabase($"exposure-computation-request-builder-{Guid.NewGuid()}")
            .Options;

        return new TestLccapDbContext(options);
    }

    private static void SeedHazardLayer(
        TestLccapDbContext db,
        Guid accountId,
        Guid planId,
        Guid hazardLayerId,
        Guid? mapAssetId,
        bool isActive,
        bool isDeleted)
    {
        _ = db.HazardLayers.Add(new HazardLayer
        {
            Id = hazardLayerId,
            AccountId = accountId,
            PlanId = planId,
            MapAssetId = mapAssetId,
            Name = "Hazard",
            HazardType = "River",
            Severity = "High",
            Source = null,
            Description = null,
            GeometryId = null,
            MetadataJson = JsonDocument.Parse("{}"),
            IsActive = isActive,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = isDeleted,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        db.SaveChanges();
    }

    private static void SeedGeoJsonLayerFeature(TestLccapDbContext db, Guid accountId, Guid mapAssetId)
    {
        _ = db.GeoJsonLayerFeatures.Add(new GeoJsonLayerFeature
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            MapAssetId = mapAssetId,
            FeatureId = "feature-1",
            FeatureType = null,
            DisplayName = null,
            PropertiesJson = JsonDocument.Parse("{\"haz\":true}"),
            GeometryJson = JsonDocument.Parse("{\"type\":\"Point\",\"coordinates\":[0,0]}"),
            StyleJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null
        });

        db.SaveChanges();
    }

    private static void SeedBarangayWithBoundaryGeoJson(TestLccapDbContext db, Guid accountId)
    {
        _ = db.Barangays.Add(new Barangay
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Barangay",
            Code = null,
            Latitude = 10m,
            Longitude = 120m,
            LandAreaHectares = null,
            Population = 100,
            Households = null,
            Classification = null,
            BoundaryGeoJson = JsonDocument.Parse("{\"type\":\"Polygon\",\"coordinates\":[]}"),
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        db.SaveChanges();
    }

    private static void SeedBarangayWithoutBoundaryGeoJson(TestLccapDbContext db, Guid accountId)
    {
        _ = db.Barangays.Add(new Barangay
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = "Barangay",
            Code = null,
            Latitude = 10m,
            Longitude = 120m,
            LandAreaHectares = null,
            Population = 100,
            Households = null,
            Classification = null,
            BoundaryGeoJson = null,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }
        });

        db.SaveChanges();
    }

    private static void SeedCriticalFacility(
        TestLccapDbContext db,
        Guid accountId,
        Guid planId,
        bool withCoordinates)
    {
        _ = db.CriticalFacilities.Add(new CriticalFacility
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = planId,
            BarangayId = null,
            Name = "Facility",
            FacilityType = "Hospital",
            Latitude = withCoordinates ? 10m : null,
            Longitude = withCoordinates ? 120m : null,
            Description = null,
            Capacity = null,
            IsEvacuationSite = false,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = null,
            CreatedByUserId = null,
            UpdatedByUserId = null,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
            RowVersion = new byte[] { 2, 2, 3, 4, 5, 6, 7, 8 }
        });

        db.SaveChanges();
    }

    private sealed class TestLccapDbContext : DbContext, ILccapDbContext
    {
        public TestLccapDbContext(DbContextOptions<TestLccapDbContext> options)
            : base(options)
        {
        }

        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<PlanSection> PlanSections => Set<PlanSection>();
        public DbSet<SectionComment> SectionComments => Set<SectionComment>();
        public DbSet<ActionItem> ActionItems => Set<ActionItem>();
        public DbSet<MonitoringIndicator> MonitoringIndicators => Set<MonitoringIndicator>();
        public DbSet<MonitoringUpdate> MonitoringUpdates => Set<MonitoringUpdate>();
        public DbSet<FileAsset> FileAssets => Set<FileAsset>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<ExportJob> ExportJobs => Set<ExportJob>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public DbSet<ClimateExpenditureTag> ClimateExpenditureTags => Set<ClimateExpenditureTag>();
        public DbSet<FundingSource> FundingSources => Set<FundingSource>();
        public DbSet<FundingProgram> FundingPrograms => Set<FundingProgram>();
        public DbSet<ActionFundingAllocation> ActionFundingAllocations => Set<ActionFundingAllocation>();

        public DbSet<Barangay> Barangays => Set<Barangay>();
        public DbSet<CriticalFacility> CriticalFacilities => Set<CriticalFacility>();
        public DbSet<MapAsset> MapAssets => Set<MapAsset>();
        public DbSet<MapAnnotation> MapAnnotations => Set<MapAnnotation>();
        public DbSet<GeoJsonLayerFeature> GeoJsonLayerFeatures => Set<GeoJsonLayerFeature>();
        public DbSet<HazardLayer> HazardLayers => Set<HazardLayer>();
        public DbSet<ExposureAnalysisJob> ExposureAnalysisJobs => Set<ExposureAnalysisJob>();
        public DbSet<ExposureSummary> ExposureSummaries => Set<ExposureSummary>();
        public DbSet<User> Users => Set<User>();
        public DbSet<NotificationEvent> NotificationEvents => Set<NotificationEvent>();
        public DbSet<UserNotification> UserNotifications => Set<UserNotification>();
        public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();
        public DbSet<CollaborationGroup> CollaborationGroups => Set<CollaborationGroup>();
        public DbSet<CollaborationGroupMember> CollaborationGroupMembers => Set<CollaborationGroupMember>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Keep EF model minimal and make JsonDocument properties scalar via conversions.
            modelBuilder.Ignore<JsonDocument>();
            modelBuilder.Ignore<PlanSection>();
            modelBuilder.Ignore<SectionComment>();
            modelBuilder.Ignore<ActionItem>();
            modelBuilder.Ignore<MonitoringIndicator>();
            modelBuilder.Ignore<MonitoringUpdate>();
            modelBuilder.Ignore<FileAsset>();
            modelBuilder.Ignore<Document>();
            modelBuilder.Ignore<AuditLog>();
            modelBuilder.Ignore<ExportJob>();
            modelBuilder.Ignore<RefreshToken>();
            modelBuilder.Ignore<ClimateExpenditureTag>();
            modelBuilder.Ignore<FundingSource>();
            modelBuilder.Ignore<FundingProgram>();
            modelBuilder.Ignore<ActionFundingAllocation>();
            modelBuilder.Ignore<MapAnnotation>();
            modelBuilder.Ignore<User>();
            modelBuilder.Ignore<NotificationEvent>();
            modelBuilder.Ignore<UserNotification>();
            modelBuilder.Ignore<NotificationTemplate>();
            modelBuilder.Ignore<CollaborationGroup>();
            modelBuilder.Ignore<CollaborationGroupMember>();

            var jsonOptions = new JsonDocumentOptions();

            var jsonDocToString = new ValueConverter<JsonDocument, string>(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, jsonOptions));

            var jsonDocNullableToString = new ValueConverter<JsonDocument?, string?>(
                v => v == null ? null : v.RootElement.GetRawText(),
                v => v == null ? null : JsonDocument.Parse(v, jsonOptions));

            modelBuilder.Entity<HazardLayer>().Property(e => e.MetadataJson).HasConversion(jsonDocToString);

            modelBuilder.Entity<GeoJsonLayerFeature>().Property(e => e.PropertiesJson).HasConversion(jsonDocToString);
            modelBuilder.Entity<GeoJsonLayerFeature>().Property(e => e.GeometryJson).HasConversion(jsonDocToString);
            modelBuilder.Entity<GeoJsonLayerFeature>().Property(e => e.StyleJson).HasConversion(jsonDocToString);

            modelBuilder.Entity<Barangay>().Property(e => e.BoundaryGeoJson).HasConversion(jsonDocNullableToString);
            modelBuilder.Entity<Barangay>().Property(e => e.MetadataJson).HasConversion(jsonDocToString);

            modelBuilder.Entity<CriticalFacility>().Property(e => e.MetadataJson).HasConversion(jsonDocToString);

            modelBuilder.Entity<MapAsset>().Property(e => e.DefaultStyleJson).HasConversion(jsonDocToString);
            modelBuilder.Entity<MapAsset>().Property(e => e.BoundsJson).HasConversion(jsonDocNullableToString);

            modelBuilder.Entity<ExposureAnalysisJob>().Property(e => e.InputJson).HasConversion(jsonDocToString);
            modelBuilder.Entity<ExposureAnalysisJob>().Property(e => e.OutputJson).HasConversion(jsonDocNullableToString);
        }
    }
}

