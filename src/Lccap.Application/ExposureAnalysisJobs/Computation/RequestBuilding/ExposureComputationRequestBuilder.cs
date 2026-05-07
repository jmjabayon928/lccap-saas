using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;

public sealed class ExposureComputationRequestBuilder : IExposureComputationRequestBuilder
{
    private sealed record HazardFeatureItem(
        string? FeatureId,
        JsonDocument GeometryJson,
        JsonDocument PropertiesJson);

    private readonly ILccapDbContext _dbContext;

    public ExposureComputationRequestBuilder(ILccapDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExposureComputationRequestBuildResult> BuildAsync(
        ExposureAnalysisJob job,
        CancellationToken cancellationToken)
    {
        var hazardLayerId = ExtractHazardLayerId(job.InputJson);
        if (hazardLayerId is null)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "HazardLayerId is required and must be a valid GUID.");
        }

        var requestedAtUtc = ExtractOptionalRequestedAtUtc(job.InputJson, out var requestedAtUtcParseOk);
        var requestedByUserId = ExtractOptionalRequestedByUserId(job.InputJson, out var requestedByUserIdParseOk);
        var mode = ExtractOptionalMode(job.InputJson, out var modeParseOk);

        if (!requestedAtUtcParseOk || !requestedByUserIdParseOk || !modeParseOk)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "One or more optional input_json fields are malformed.");
        }

        var hazardLayer = await _dbContext.HazardLayers
            .AsNoTracking()
            .SingleOrDefaultAsync(
                h => h.Id == hazardLayerId && h.AccountId == job.AccountId && h.PlanId == job.PlanId && !h.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (hazardLayer is null || !hazardLayer.IsActive)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "Hazard layer is missing, inactive, or not accessible for this tenant.");
        }

        if (hazardLayer.MapAssetId is null)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "Hazard layer has no associated map asset.");
        }

        var hazardFeatures = await _dbContext.GeoJsonLayerFeatures
            .AsNoTracking()
            .Where(g => g.MapAssetId == hazardLayer.MapAssetId.Value && g.AccountId == job.AccountId && !g.IsDeleted)
            .Select(g => new HazardFeatureItem(
                g.FeatureId,
                g.GeometryJson,
                g.PropertiesJson))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hazardFeatures.Count == 0)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "No hazard features exist for the selected hazard layer.");
        }

        var hazardFeaturesDoc = BuildHazardFeaturesFeatureCollection(hazardFeatures);

        var barangays = await _dbContext.Barangays
            .AsNoTracking()
            .Where(b => b.AccountId == job.AccountId && !b.IsDeleted && b.BoundaryGeoJson != null)
            .Select(b => new
            {
                b.Id,
                b.Population,
                b.BoundaryGeoJson,
                b.Latitude,
                b.Longitude
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (barangays.Count == 0)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "No barangay boundaries with GeoJSON are available for this account.");
        }

        var facilities = await _dbContext.CriticalFacilities
            .AsNoTracking()
            .Where(cf => cf.AccountId == job.AccountId && cf.PlanId == job.PlanId && !cf.IsDeleted)
            .Select(cf => new
            {
                cf.Id,
                cf.BarangayId,
                cf.FacilityType,
                cf.Capacity,
                cf.Latitude,
                cf.Longitude
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var facilitiesWithCoords = facilities.Where(f => f.Latitude != null && f.Longitude != null).ToList();
        if (facilitiesWithCoords.Count == 0)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "No critical facilities have usable latitude/longitude coordinates.");
        }

        var request = new ExposureComputationServiceRequest(
            JobId: job.Id,
            AccountId: job.AccountId,
            PlanId: job.PlanId,
            HazardLayerId: hazardLayer.Id,
            Mode: mode,
            RequestedAtUtc: requestedAtUtc,
            RequestedByUserId: requestedByUserId,
            ComputationVersion: ExposureComputationContractVersion.Current,
            CrsPolicy: ExposureComputationCrsPolicy.RequireExplicitEpsg4326(),
            GeometryPolicy: ExposureComputationGeometryPolicy.FailFastNoRepair(),
            HazardLayer: new ExposureComputationHazardLayer(
                Id: hazardLayer.Id,
                HazardType: hazardLayer.HazardType,
                Severity: hazardLayer.Severity,
                MapAssetId: hazardLayer.MapAssetId),
            HazardFeatures: hazardFeaturesDoc,
            Barangays: barangays.Select(b => new ExposureComputationBarangay(
                    Id: b.Id,
                    Population: b.Population,
                    BoundaryGeoJson: b.BoundaryGeoJson,
                    Latitude: b.Latitude,
                    Longitude: b.Longitude))
                .ToList(),
            CriticalFacilities: facilities.Select(f => new ExposureComputationCriticalFacility(
                    Id: f.Id,
                    BarangayId: f.BarangayId,
                    FacilityType: f.FacilityType,
                    Capacity: f.Capacity,
                    Latitude: f.Latitude,
                    Longitude: f.Longitude))
                .ToList());

        var validationErrors = ExposureComputationPayloadValidation.ValidateRequest(request);
        if (validationErrors.Count != 0)
        {
            return ExposureComputationRequestBuildResult.Failed(
                ExposureComputationEngineErrorCode.ValidationFailed,
                "Generated computation request is invalid.",
                validationErrors);
        }

        return ExposureComputationRequestBuildResult.Success(request);
    }

    private static Guid? ExtractHazardLayerId(JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("hazardLayerId", out var hazardLayerIdElement))
        {
            return null;
        }

        var hazardLayerIdString = hazardLayerIdElement.GetString();
        return hazardLayerIdString is not null && Guid.TryParse(hazardLayerIdString, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? ExtractOptionalRequestedAtUtc(JsonDocument inputJson)
    {
        // Backward-compatible extraction: if the field is missing return null.
        // If the field exists but is malformed return null and mark parse failure.
        // Parse failure is reported via the out parameter overload below.
        var unused = false;
        return ExtractOptionalRequestedAtUtc(inputJson, out unused);
    }

    private static DateTimeOffset? ExtractOptionalRequestedAtUtc(JsonDocument inputJson, out bool parseOk)
    {
        parseOk = true;
        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            parseOk = false;
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("requestedAtUtc", out var requestedAtUtcElement))
        {
            return null;
        }

        if (requestedAtUtcElement.ValueKind != JsonValueKind.String) { parseOk = false; return null; }

        var requestedAtUtcString = requestedAtUtcElement.GetString();
        if (requestedAtUtcString is null) { parseOk = false; return null; }

        if (!DateTimeOffset.TryParse(requestedAtUtcString, out var parsed))
        {
            parseOk = false;
            return null;
        }

        return parsed;
    }

    private static Guid? ExtractOptionalRequestedByUserId(JsonDocument inputJson, out bool parseOk)
    {
        parseOk = true;
        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            parseOk = false;
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("requestedByUserId", out var requestedByUserIdElement))
        {
            return null;
        }

        if (requestedByUserIdElement.ValueKind != JsonValueKind.String) { parseOk = false; return null; }

        var requestedByUserIdString = requestedByUserIdElement.GetString();
        if (requestedByUserIdString is null) { parseOk = false; return null; }

        if (!Guid.TryParse(requestedByUserIdString, out var parsed))
        {
            parseOk = false;
            return null;
        }

        return parsed;
    }

    private static string? ExtractOptionalMode(JsonDocument inputJson, out bool parseOk)
    {
        parseOk = true;
        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            parseOk = false;
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("mode", out var modeElement))
        {
            return null;
        }

        if (modeElement.ValueKind != JsonValueKind.String) { parseOk = false; return null; }

        var mode = modeElement.GetString();
        if (mode is null) { parseOk = false; return null; }

        var trimmed = mode.Trim();
        if (trimmed.Length == 0)
        {
            parseOk = false;
            return null;
        }

        return trimmed;
    }

    private static JsonDocument BuildHazardFeaturesFeatureCollection(
        IReadOnlyList<HazardFeatureItem> hazardFeatures)
    {
        var featureParts = new List<string>(hazardFeatures.Count);
        foreach (var feature in hazardFeatures)
        {
            var idJson = feature.FeatureId is null ? "null" : JsonSerializer.Serialize(feature.FeatureId);
            var geometryRaw = feature.GeometryJson.RootElement.GetRawText();
            var propertiesRaw = feature.PropertiesJson.RootElement.GetRawText();
            featureParts.Add(
                $"{{\"type\":\"Feature\",\"id\":{idJson},\"geometry\":{geometryRaw},\"properties\":{propertiesRaw}}}");
        }

        var featuresArray = string.Join(",", featureParts);
        var json = $"{{\"type\":\"FeatureCollection\",\"features\":[{featuresArray}]}}";
        return JsonDocument.Parse(json);
    }
}

