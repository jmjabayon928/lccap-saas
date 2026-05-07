using System.Text.Json;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationHazardLayer(
    Guid Id,
    string HazardType,
    string Severity,
    Guid? MapAssetId);

public sealed record ExposureComputationBarangay(
    Guid Id,
    int? Population,
    JsonDocument? BoundaryGeoJson,
    decimal? Latitude,
    decimal? Longitude);

public sealed record ExposureComputationCriticalFacility(
    Guid Id,
    Guid? BarangayId,
    string FacilityType,
    int? Capacity,
    decimal? Latitude,
    decimal? Longitude);

public sealed record ExposureComputationServiceRequest(
    Guid JobId,
    Guid AccountId,
    Guid PlanId,
    Guid HazardLayerId,
    string? Mode,
    DateTimeOffset? RequestedAtUtc,
    Guid? RequestedByUserId,
    string ComputationVersion,
    ExposureComputationCrsPolicy? CrsPolicy,
    ExposureComputationGeometryPolicy? GeometryPolicy,
    ExposureComputationHazardLayer? HazardLayer,
    JsonDocument? HazardFeatures,
    IReadOnlyList<ExposureComputationBarangay>? Barangays,
    IReadOnlyList<ExposureComputationCriticalFacility>? CriticalFacilities);

