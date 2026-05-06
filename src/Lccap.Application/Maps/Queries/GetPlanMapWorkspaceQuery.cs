using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Maps.Queries;

public sealed class GetPlanMapWorkspaceQuery
{
    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanMapWorkspaceQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanMapWorkspaceResult> Execute(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanMapWorkspaceResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanMapWorkspaceResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var featureCounts = await (
                from g in _dbContext.GeoJsonLayerFeatures.AsNoTracking()
                join m in _dbContext.MapAssets.AsNoTracking() on g.MapAssetId equals m.Id
                join f in _dbContext.FileAssets.AsNoTracking() on m.FileAssetId equals f.Id
                where g.AccountId == accountId
                      && !g.IsDeleted
                      && m.AccountId == accountId
                      && m.PlanId == planId
                      && !m.IsDeleted
                      && f.AccountId == accountId
                      && !f.IsDeleted
                group g by g.MapAssetId
                into grp
                select new
                {
                    MapAssetId = grp.Key,
                    FeatureCount = grp.Count()
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var featureCountLookup = featureCounts.ToDictionary(x => x.MapAssetId, x => x.FeatureCount);

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetPlanMapWorkspaceResult.NotFound();
        }

        var mapAssetRows = await (
                from m in _dbContext.MapAssets.AsNoTracking()
                join f in _dbContext.FileAssets.AsNoTracking() on m.FileAssetId equals f.Id
                where m.AccountId == accountId
                      && m.PlanId == planId
                      && !m.IsDeleted
                      && f.AccountId == accountId
                      && !f.IsDeleted
                orderby m.CreatedAtUtc descending
                select new
                {
                    m.Id,
                    m.Name,
                    m.MapType,
                    m.MapFormat,
                    m.Description,
                    BoundsJson = m.BoundsJson,
                    DefaultStyleJson = m.DefaultStyleJson,
                    f.OriginalFileName,
                    f.ContentType,
                    f.FileSizeBytes,
                    m.CreatedAtUtc
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var mapSummaries = mapAssetRows.ConvertAll(a => new MapAssetWorkspaceSummaryDto(
            a.Id,
            a.Name,
            a.MapType,
            a.MapFormat,
            a.Description,
            a.BoundsJson,
            a.DefaultStyleJson,
            a.OriginalFileName,
            a.ContentType,
            a.FileSizeBytes,
            a.CreatedAtUtc,
            featureCountLookup.TryGetValue(a.Id, out var c) ? c : 0));

        var barangayRows = await _dbContext.Barangays.AsNoTracking()
            .Where(b => b.AccountId == accountId && !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BarangayWorkspaceSummaryDto(
                b.Id,
                b.Name,
                b.Code,
                b.Latitude,
                b.Longitude,
                b.LandAreaHectares,
                b.Population,
                b.Households,
                b.Classification))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var facilityRows = await (
                from cf in _dbContext.CriticalFacilities.AsNoTracking()
                join b in _dbContext.Barangays.AsNoTracking() on cf.BarangayId equals b.Id into bj
                from bOpt in bj.DefaultIfEmpty()
                where cf.AccountId == accountId && cf.PlanId == planId && !cf.IsDeleted
                orderby cf.FacilityType, cf.Name
                select new CriticalFacilityWorkspaceSummaryDto(
                    cf.Id,
                    cf.Name,
                    cf.FacilityType,
                    cf.BarangayId,
                    bOpt != null && !bOpt.IsDeleted ? bOpt.Name : null,
                    cf.Latitude,
                    cf.Longitude,
                    cf.Capacity,
                    cf.IsEvacuationSite,
                    cf.Description))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var counts = await ComputeCountsAsync(planId, accountId, cancellationToken).ConfigureAwait(false);

        var dto = new PlanMapWorkspaceDto(planId, mapSummaries, barangayRows, facilityRows, counts);
        return GetPlanMapWorkspaceResult.Success(dto);
    }

    private async Task<PlanMapWorkspaceCountsDto> ComputeCountsAsync(
        Guid planId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var mapAssets = await (
                from m in _dbContext.MapAssets.AsNoTracking()
                join f in _dbContext.FileAssets.AsNoTracking() on m.FileAssetId equals f.Id
                where m.AccountId == accountId
                      && m.PlanId == planId
                      && !m.IsDeleted
                      && f.AccountId == accountId
                      && !f.IsDeleted
                select m.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var geoJsonLayers = await (
                from m in _dbContext.MapAssets.AsNoTracking()
                join f in _dbContext.FileAssets.AsNoTracking() on m.FileAssetId equals f.Id
                where m.AccountId == accountId
                      && m.PlanId == planId
                      && !m.IsDeleted
                      && m.MapFormat == "GeoJson"
                      && f.AccountId == accountId
                      && !f.IsDeleted
                select m.Id)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);

        var barangays = await _dbContext.Barangays.AsNoTracking()
            .CountAsync(b => b.AccountId == accountId && !b.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        var criticalFacilities = await _dbContext.CriticalFacilities.AsNoTracking()
            .CountAsync(c => c.AccountId == accountId && c.PlanId == planId && !c.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

        var evacuationSites = await _dbContext.CriticalFacilities.AsNoTracking()
            .CountAsync(
                c => c.AccountId == accountId && c.PlanId == planId && !c.IsDeleted && c.IsEvacuationSite,
                cancellationToken)
            .ConfigureAwait(false);

        return new PlanMapWorkspaceCountsDto(mapAssets, geoJsonLayers, barangays, criticalFacilities, evacuationSites);
    }
}

public sealed class GetPlanMapWorkspaceResult
{
    private GetPlanMapWorkspaceResult(bool isSuccess, int statusCode, PlanMapWorkspaceDto? workspace)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Workspace = workspace;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public PlanMapWorkspaceDto? Workspace { get; }

    public static GetPlanMapWorkspaceResult Success(PlanMapWorkspaceDto workspace) => new(true, 200, workspace);

    public static GetPlanMapWorkspaceResult Unauthorized() => new(false, 401, null);

    public static GetPlanMapWorkspaceResult Forbidden() => new(false, 403, null);

    public static GetPlanMapWorkspaceResult NotFound() => new(false, 404, null);
}

public sealed record PlanMapWorkspaceDto(
    Guid PlanId,
    IReadOnlyList<MapAssetWorkspaceSummaryDto> MapAssets,
    IReadOnlyList<BarangayWorkspaceSummaryDto> Barangays,
    IReadOnlyList<CriticalFacilityWorkspaceSummaryDto> CriticalFacilities,
    PlanMapWorkspaceCountsDto Counts);

public sealed record PlanMapWorkspaceCountsDto(
    int MapAssets,
    int GeoJsonLayers,
    int Barangays,
    int CriticalFacilities,
    int EvacuationSites);

public sealed record MapAssetWorkspaceSummaryDto(
    Guid Id,
    string Name,
    string MapType,
    string MapFormat,
    string? Description,
    JsonDocument? BoundsJson,
    JsonDocument DefaultStyleJson,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc,
    int FeatureCount);

public sealed record BarangayWorkspaceSummaryDto(
    Guid Id,
    string Name,
    string? Code,
    decimal? Latitude,
    decimal? Longitude,
    decimal? LandAreaHectares,
    int? Population,
    int? Households,
    string? Classification);

public sealed record CriticalFacilityWorkspaceSummaryDto(
    Guid Id,
    string Name,
    string FacilityType,
    Guid? BarangayId,
    string? BarangayName,
    decimal? Latitude,
    decimal? Longitude,
    int? Capacity,
    bool IsEvacuationSite,
    string? Description);
