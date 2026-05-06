using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Maps.Queries;

public sealed class GetGeoJsonLayerFeaturesQuery
{
    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public GetGeoJsonLayerFeaturesQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetGeoJsonLayerFeaturesResult> Execute(
        Guid mapAssetId,
        int limit = 500,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetGeoJsonLayerFeaturesResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetGeoJsonLayerFeaturesResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var take = limit < 1 ? 1 : limit > 500 ? 500 : limit;

        var assetOk = await _dbContext.MapAssets.AsNoTracking().AnyAsync(
                m => m.Id == mapAssetId && m.AccountId == accountId && !m.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!assetOk)
        {
            return GetGeoJsonLayerFeaturesResult.NotFound();
        }

        var items = await _dbContext.GeoJsonLayerFeatures.AsNoTracking()
            .Where(g => g.MapAssetId == mapAssetId && g.AccountId == accountId && !g.IsDeleted)
            .OrderBy(g => g.DisplayName ?? string.Empty).ThenBy(g => g.FeatureId ?? string.Empty).ThenBy(g => g.CreatedAtUtc)
            .Take(take)
            .Select(g => new GeoJsonLayerFeatureListItemDto(
                g.Id,
                g.MapAssetId,
                g.FeatureId,
                g.FeatureType,
                g.DisplayName,
                g.PropertiesJson,
                g.GeometryJson,
                g.StyleJson,
                g.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return GetGeoJsonLayerFeaturesResult.Success(items);
    }
}

public sealed class GetGeoJsonLayerFeaturesResult
{
    private GetGeoJsonLayerFeaturesResult(bool isSuccess, int statusCode, IReadOnlyList<GeoJsonLayerFeatureListItemDto>? features)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Features = features;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public IReadOnlyList<GeoJsonLayerFeatureListItemDto>? Features { get; }

    public static GetGeoJsonLayerFeaturesResult Success(IReadOnlyList<GeoJsonLayerFeatureListItemDto> features) =>
        new(true, 200, features);

    public static GetGeoJsonLayerFeaturesResult Unauthorized() => new(false, 401, null);

    public static GetGeoJsonLayerFeaturesResult Forbidden() => new(false, 403, null);

    public static GetGeoJsonLayerFeaturesResult NotFound() => new(false, 404, null);
}

public sealed record GeoJsonLayerFeatureListItemDto(
    Guid Id,
    Guid MapAssetId,
    string? FeatureId,
    string? FeatureType,
    string? DisplayName,
    JsonDocument PropertiesJson,
    JsonDocument GeometryJson,
    JsonDocument StyleJson,
    DateTimeOffset CreatedAtUtc);
