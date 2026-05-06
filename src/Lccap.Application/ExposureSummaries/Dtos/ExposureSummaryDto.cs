namespace Lccap.Application.ExposureSummaries.Dtos;

public sealed record ExposureSummaryDto(
    Guid Id,
    Guid PlanId,
    Guid? ExposureAnalysisJobId,
    Guid? BarangayId,
    Guid? CriticalFacilityId,
    Guid? HazardLayerId,
    string HazardType,
    string? Severity,
    decimal? ExposedAreaHectares,
    int ExposedFacilityCount,
    int? ExposedPopulation,
    decimal? RiskScore,
    System.Text.Json.JsonDocument SummaryJson,
    DateTimeOffset CreatedAtUtc);

