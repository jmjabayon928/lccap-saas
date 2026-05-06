using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Maps.Commands;

public sealed class CreateGeoJsonLayerCommand
{
    public const int MaxFeatureCount = 500;

    public const int MaxGeoJsonPayloadChars = 2_000_000;

    public const int MaxDefaultStyleJsonChars = 50_000;

    public const int MaxBoundsJsonChars = 50_000;

    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> AllowedMapTypes = new(StringComparer.Ordinal)
    {
        "Flood",
        "Landslide",
        "StormSurge",
        "Boundary",
        "LandUse",
        "Hazard",
        "Other",
    };

    private static readonly HashSet<string> AllowedGeometryTypes = new(StringComparer.Ordinal)
    {
        "Point",
        "MultiPoint",
        "LineString",
        "MultiLineString",
        "Polygon",
        "MultiPolygon",
    };

    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public CreateGeoJsonLayerCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreateGeoJsonLayerResult> Execute(
        Guid planId,
        CreateGeoJsonLayerRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.GeoJson);

        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return CreateGeoJsonLayerResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > 250)
        {
            return CreateGeoJsonLayerResult.BadRequest(new[] { "Name is required and must be at most 250 characters." });
        }

        var mapType = request.MapType?.Trim() ?? string.Empty;
        if (!AllowedMapTypes.Contains(mapType))
        {
            return CreateGeoJsonLayerResult.BadRequest(new[] { "Invalid mapType." });
        }

        try
        {
            var rawLen = request.GeoJson.RootElement.GetRawText().Length;
            if (rawLen > MaxGeoJsonPayloadChars)
            {
                return CreateGeoJsonLayerResult.BadRequest(new[] { "GeoJSON payload exceeds maximum allowed size." });
            }
        }
        catch (ObjectDisposedException)
        {
            return CreateGeoJsonLayerResult.BadRequest(new[] { "GeoJSON payload is invalid." });
        }

        if (!TryParseFeatureRows(request.GeoJson, out var parsedRows, out var parseError))
        {
            return CreateGeoJsonLayerResult.BadRequest(new[] { parseError ?? "Invalid GeoJSON." });
        }

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);
        if (!planOk)
        {
            return CreateGeoJsonLayerResult.NotFound();
        }

        var fileExists = await _dbContext.FileAssets.AsNoTracking().AnyAsync(
                f => f.Id == request.FileAssetId && f.AccountId == accountId && !f.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);
        if (!fileExists)
        {
            return CreateGeoJsonLayerResult.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        if (request.DefaultStyleJson is not null)
        {
            try
            {
                var defaultStyleRawLen = request.DefaultStyleJson.RootElement.GetRawText().Length;
                if (defaultStyleRawLen > MaxDefaultStyleJsonChars)
                {
                    return CreateGeoJsonLayerResult.BadRequest(new[] { "DefaultStyleJson exceeds maximum allowed size." });
                }
            }
            catch (ObjectDisposedException)
            {
                return CreateGeoJsonLayerResult.BadRequest(new[] { "DefaultStyleJson is invalid." });
            }
        }

        if (request.BoundsJson is not null)
        {
            try
            {
                var boundsRawLen = request.BoundsJson.RootElement.GetRawText().Length;
                if (boundsRawLen > MaxBoundsJsonChars)
                {
                    return CreateGeoJsonLayerResult.BadRequest(new[] { "BoundsJson exceeds maximum allowed size." });
                }
            }
            catch (ObjectDisposedException)
            {
                return CreateGeoJsonLayerResult.BadRequest(new[] { "BoundsJson is invalid." });
            }
        }

        JsonDocument? boundsDoc = request.BoundsJson is null
            ? null
            : JsonDocument.Parse(request.BoundsJson.RootElement.GetRawText());

        JsonDocument defaultStyleDoc = request.DefaultStyleJson is null
            ? JsonDocument.Parse("{}")
            : JsonDocument.Parse(request.DefaultStyleJson.RootElement.GetRawText());

        var mapAsset = new MapAsset
        {
            AccountId = accountId,
            PlanId = planId,
            FileAssetId = request.FileAssetId,
            Name = name,
            MapType = mapType,
            MapFormat = "GeoJson",
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            BoundsJson = boundsDoc,
            DefaultStyleJson = defaultStyleDoc,
            UploadedByUserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
        };
        mapAsset.EnsureRowVersion();
        _ = _dbContext.MapAssets.Add(mapAsset);

        foreach (var row in parsedRows)
        {
            var feature = new GeoJsonLayerFeature
            {
                AccountId = accountId,
                MapAsset = mapAsset,
                FeatureId = row.FeatureId,
                FeatureType = row.GeometryType,
                DisplayName = row.DisplayName,
                PropertiesJson = row.PropertiesJson,
                GeometryJson = row.GeometryJson,
                StyleJson = row.StyleJson,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedByUserId = userId,
                UpdatedByUserId = userId,
                IsDeleted = false,
            };
            feature.EnsureRowVersion();
            _ = _dbContext.GeoJsonLayerFeatures.Add(feature);
        }

        var newAuditValues = JsonSerializer.SerializeToDocument(
            new { mapAsset.Id, mapAsset.PlanId, FeatureCount = parsedRows.Count, mapAsset.MapFormat, mapAsset.MapType },
            AuditJsonOptions);
        var metadata = JsonSerializer.SerializeToDocument(new { planId }, AuditJsonOptions);
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "MapAsset",
            EntityId = mapAsset.Id,
            Action = "GeoJsonLayerCreated",
            OldValuesJson = null,
            NewValuesJson = newAuditValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        _ = _dbContext.AuditLogs.Add(audit);

        _ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var fileHead = await _dbContext.FileAssets.AsNoTracking()
            .Where(f => f.Id == mapAsset.FileAssetId)
            .Select(f => new { f.OriginalFileName, f.ContentType, f.FileSizeBytes })
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        var summary = new CreatedGeoJsonMapAssetSummaryDto(
            mapAsset.Id,
            mapAsset.Name,
            mapAsset.MapType,
            mapAsset.MapFormat,
            mapAsset.Description,
            parsedRows.Count,
            fileHead.OriginalFileName,
            fileHead.ContentType,
            fileHead.FileSizeBytes,
            mapAsset.CreatedAtUtc);

        return CreateGeoJsonLayerResult.Created(summary);
    }

    private bool TryParseFeatureRows(JsonDocument geoJson, out List<ParsedGeoJsonFeatureRow> rows, out string? error)
    {
        rows = [];
        error = null;

        try
        {
            var root = geoJson.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "GeoJSON root must be an object.";
                return false;
            }

            if (!root.TryGetProperty("type", out var typeProp) || typeProp.ValueKind != JsonValueKind.String)
            {
                error = "GeoJSON must include a string type property.";
                return false;
            }

            if (!string.Equals(typeProp.GetString(), "FeatureCollection", StringComparison.Ordinal))
            {
                error = "GeoJSON must be a FeatureCollection.";
                return false;
            }

            if (!root.TryGetProperty("features", out var featuresEl) || featuresEl.ValueKind != JsonValueKind.Array)
            {
                error = "GeoJSON must include a features array.";
                return false;
            }

            var count = featuresEl.GetArrayLength();
            if (count > MaxFeatureCount)
            {
                error = $"GeoJSON must not contain more than {MaxFeatureCount} features.";
                return false;
            }

            foreach (var featureEl in featuresEl.EnumerateArray())
            {
                if (featureEl.ValueKind != JsonValueKind.Object)
                {
                    error = "Each feature must be a GeoJSON object.";
                    return false;
                }

                JsonElement propertiesEl = default;
                var hasProperties = featureEl.TryGetProperty("properties", out propertiesEl);

                if (!featureEl.TryGetProperty("geometry", out var geometryEl) || geometryEl.ValueKind != JsonValueKind.Object)
                {
                    error = "Each feature must include a geometry object.";
                    return false;
                }

                if (!geometryEl.TryGetProperty("type", out var geomTypeEl) || geomTypeEl.ValueKind != JsonValueKind.String)
                {
                    error = "Each geometry must include a string type.";
                    return false;
                }

                var geomType = geomTypeEl.GetString() ?? string.Empty;
                if (!AllowedGeometryTypes.Contains(geomType))
                {
                    error = $"Geometry type '{geomType}' is not allowed.";
                    return false;
                }

                JsonDocument propertiesDoc;
                if (hasProperties)
                {
                    if (propertiesEl.ValueKind == JsonValueKind.Null)
                    {
                        propertiesDoc = JsonDocument.Parse("{}");
                    }
                    else if (propertiesEl.ValueKind == JsonValueKind.Object)
                    {
                        propertiesDoc = JsonDocument.Parse(propertiesEl.GetRawText());
                    }
                    else
                    {
                        error = "Feature properties must be an object when provided.";
                        return false;
                    }
                }
                else
                {
                    propertiesDoc = JsonDocument.Parse("{}");
                }

                JsonDocument styleDoc;
                if (propertiesDoc.RootElement.TryGetProperty("style", out var styleEl) && styleEl.ValueKind == JsonValueKind.Object)
                {
                    styleDoc = JsonDocument.Parse(styleEl.GetRawText());
                }
                else
                {
                    styleDoc = JsonDocument.Parse("{}");
                }

                var featureId = ExtractFeatureId(featureEl);
                JsonElement propsForDisplay = propertiesDoc.RootElement;
                var displayName = ExtractDisplayName(propsForDisplay, featureId, featureEl);
                var geometryDoc = JsonDocument.Parse(geometryEl.GetRawText());

                rows.Add(new ParsedGeoJsonFeatureRow(featureId, geomType, displayName, propertiesDoc, geometryDoc, styleDoc));
            }
        }
        catch (JsonException)
        {
            error = "GeoJSON parsing failed.";
            return false;
        }

        return true;
    }

    private static string? ExtractFeatureId(JsonElement featureEl)
    {
        if (!featureEl.TryGetProperty("id", out var idEl))
        {
            return null;
        }

        // Contract: only string/number become feature_id; booleans/objects/arrays/null become null.
        string? raw = idEl.ValueKind switch
        {
            JsonValueKind.String => idEl.GetString(),
            JsonValueKind.Number => idEl.GetRawText(),
            _ => null
        };

        if (string.IsNullOrEmpty(raw))
        {
            return null;
        }

        return raw.Length <= 150 ? raw : raw[..150];
    }

    private static string? ExtractDisplayName(JsonElement propertiesEl, string? featureId, JsonElement featureEl)
    {
        string? fromProps = null;
        if (propertiesEl.ValueKind == JsonValueKind.Object)
        {
            if (propertiesEl.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String)
            {
                fromProps = n.GetString();
            }
            else if (propertiesEl.TryGetProperty("Name", out var n2) && n2.ValueKind == JsonValueKind.String)
            {
                fromProps = n2.GetString();
            }
            else if (propertiesEl.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
            {
                fromProps = t.GetString();
            }
        }

        if (!string.IsNullOrWhiteSpace(fromProps))
        {
            var s = fromProps.Trim();
            return s.Length <= 250 ? s : s[..250];
        }

        if (!string.IsNullOrEmpty(featureId))
        {
            return featureId.Length <= 250 ? featureId : featureId[..250];
        }

        if (featureEl.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
        {
            var s = idEl.GetString()?.Trim();
            if (!string.IsNullOrEmpty(s))
            {
                return s.Length <= 250 ? s : s[..250];
            }
        }

        return null;
    }

    private sealed record ParsedGeoJsonFeatureRow(
        string? FeatureId,
        string GeometryType,
        string? DisplayName,
        JsonDocument PropertiesJson,
        JsonDocument GeometryJson,
        JsonDocument StyleJson);
}

public sealed record CreateGeoJsonLayerRequest(
    Guid FileAssetId,
    string Name,
    string MapType,
    string? Description,
    JsonDocument GeoJson,
    JsonDocument? DefaultStyleJson,
    JsonDocument? BoundsJson);

public sealed record CreatedGeoJsonMapAssetSummaryDto(
    Guid Id,
    string Name,
    string MapType,
    string MapFormat,
    string? Description,
    int FeatureCount,
    string OriginalFileName,
    string ContentType,
    long FileSizeBytes,
    DateTimeOffset CreatedAtUtc);

public sealed class CreateGeoJsonLayerResult
{
    private CreateGeoJsonLayerResult(int statusCode, IReadOnlyList<string>? errors, CreatedGeoJsonMapAssetSummaryDto? summary)
    {
        StatusCode = statusCode;
        Errors = errors;
        Summary = summary;
    }

    public int StatusCode { get; }

    public IReadOnlyList<string>? Errors { get; }

    public CreatedGeoJsonMapAssetSummaryDto? Summary { get; }

    public static CreateGeoJsonLayerResult Created(CreatedGeoJsonMapAssetSummaryDto summary) =>
        new(201, null, summary);

    public static CreateGeoJsonLayerResult BadRequest(IReadOnlyList<string> errors) =>
        new(400, errors, null);

    public static CreateGeoJsonLayerResult Forbidden() => new(403, null, null);

    public static CreateGeoJsonLayerResult NotFound() => new(404, null, null);
}
