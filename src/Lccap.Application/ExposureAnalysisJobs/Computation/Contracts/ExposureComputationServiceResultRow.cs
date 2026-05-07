using System.Text.Json;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationServiceResultRow(
    Guid? BarangayId,
    Guid? CriticalFacilityId,
    Guid? HazardLayerId,
    string HazardType,
    string? Severity,
    decimal? ExposedAreaHectares,
    int ExposedFacilityCount,
    int? ExposedPopulation,
    decimal? RiskScore,
    JsonDocument SummaryJson);

