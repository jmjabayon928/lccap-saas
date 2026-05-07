using System.Text.Json;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Xunit;

namespace Lccap.Application.Tests.ExposureAnalysisJobs;

public sealed class ExposureComputationContractTests
{
    [Fact]
    public void Default_crs_policy_requires_explicit_epsg_4326_and_fails_on_ambiguity()
    {
        var policy = ExposureComputationCrsPolicy.RequireExplicitEpsg4326();

        Assert.Equal("Explicit", policy.SourceType);
        Assert.Equal(4326, policy.SourceEpsg);
        Assert.Equal("Explicit", policy.TargetType);
        Assert.Equal(4326, policy.TargetEpsg);
        Assert.True(policy.FailOnAmbiguity);
    }

    [Fact]
    public void Default_geometry_policy_is_fail_fast_no_repair()
    {
        var policy = ExposureComputationGeometryPolicy.FailFastNoRepair();

        Assert.True(policy.FailOnInvalidGeoJson);
        Assert.True(policy.FailOnUnsupportedGeometryTypes);
        Assert.True(policy.FailOnEmptyGeometry);
        Assert.Equal("None", policy.RepairStrategy);
    }

    [Fact]
    public void Failed_response_with_results_is_invalid()
    {
        using var summaryJson = JsonDocument.Parse("{}");

        var row = new ExposureComputationServiceResultRow(
            BarangayId: null,
            CriticalFacilityId: null,
            HazardLayerId: null,
            HazardType: "River",
            Severity: "High",
            ExposedAreaHectares: 1,
            ExposedFacilityCount: 0,
            ExposedPopulation: 10,
            RiskScore: 0,
            SummaryJson: summaryJson);

        var response = new ExposureComputationServiceResponse(
            Success: false,
            EngineName: "NotConfigured",
            EngineVersion: null,
            ComputationRunId: null,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            ErrorCode: "EngineUnavailable",
            ErrorMessage: "engine down",
            Diagnostics: new ExposureComputationDiagnostics(
                Message: null,
                Warnings: Array.Empty<string>(),
                ValidationNotes: Array.Empty<string>(),
                GeometryFeatureCount: null,
                BarangayCount: null,
                CriticalFacilityCount: null,
                CrsDescription: null),
            Results: new[] { row });

        var errors = ExposureComputationPayloadValidation.ValidateResponse(response);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Successful_response_with_negative_metrics_is_invalid()
    {
        using var summaryJson = JsonDocument.Parse("{}");

        var row = new ExposureComputationServiceResultRow(
            BarangayId: null,
            CriticalFacilityId: null,
            HazardLayerId: null,
            HazardType: "River",
            Severity: "High",
            ExposedAreaHectares: -1,
            ExposedFacilityCount: -1,
            ExposedPopulation: -10,
            RiskScore: -5,
            SummaryJson: summaryJson);

        var response = new ExposureComputationServiceResponse(
            Success: true,
            EngineName: "Engine",
            EngineVersion: "1.0",
            ComputationRunId: "run-1",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            ErrorCode: null,
            ErrorMessage: null,
            Diagnostics: new ExposureComputationDiagnostics(
                Message: null,
                Warnings: Array.Empty<string>(),
                ValidationNotes: Array.Empty<string>(),
                GeometryFeatureCount: null,
                BarangayCount: null,
                CriticalFacilityCount: null,
                CrsDescription: null),
            Results: new[] { row });

        var errors = ExposureComputationPayloadValidation.ValidateResponse(response);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Successful_response_with_valid_result_is_valid()
    {
        using var summaryJson = JsonDocument.Parse("{}");

        var row = new ExposureComputationServiceResultRow(
            BarangayId: null,
            CriticalFacilityId: null,
            HazardLayerId: null,
            HazardType: "River",
            Severity: "High",
            ExposedAreaHectares: 0,
            ExposedFacilityCount: 0,
            ExposedPopulation: 0,
            RiskScore: 0,
            SummaryJson: summaryJson);

        var response = new ExposureComputationServiceResponse(
            Success: true,
            EngineName: "Engine",
            EngineVersion: "1.0",
            ComputationRunId: "run-1",
            CompletedAtUtc: DateTimeOffset.UtcNow,
            ErrorCode: null,
            ErrorMessage: null,
            Diagnostics: new ExposureComputationDiagnostics(
                Message: null,
                Warnings: Array.Empty<string>(),
                ValidationNotes: Array.Empty<string>(),
                GeometryFeatureCount: null,
                BarangayCount: null,
                CriticalFacilityCount: null,
                CrsDescription: null),
            Results: new[] { row });

        var errors = ExposureComputationPayloadValidation.ValidateResponse(response);
        Assert.Empty(errors);
    }

    [Fact]
    public void Request_missing_identity_fields_is_invalid()
    {
        using var hazardFeatures = JsonDocument.Parse("{\"type\":\"FeatureCollection\",\"features\":[]}");
        var crsPolicy = ExposureComputationCrsPolicy.RequireExplicitEpsg4326();
        var geometryPolicy = ExposureComputationGeometryPolicy.FailFastNoRepair();

        var request = new ExposureComputationServiceRequest(
            JobId: Guid.Empty,
            AccountId: Guid.Empty,
            PlanId: Guid.Empty,
            HazardLayerId: Guid.Empty,
            Mode: null,
            RequestedAtUtc: null,
            RequestedByUserId: null,
            ComputationVersion: "2026-05-06.v1",
            CrsPolicy: crsPolicy,
            GeometryPolicy: geometryPolicy,
            HazardLayer: new ExposureComputationHazardLayer(
                Id: Guid.NewGuid(),
                HazardType: "River",
                Severity: "High",
                MapAssetId: null),
            HazardFeatures: hazardFeatures,
            Barangays: Array.Empty<ExposureComputationBarangay>(),
            CriticalFacilities: Array.Empty<ExposureComputationCriticalFacility>());

        var errors = ExposureComputationPayloadValidation.ValidateRequest(request);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Request_with_required_contract_fields_is_valid()
    {
        using var hazardFeatures = JsonDocument.Parse("{\"type\":\"FeatureCollection\",\"features\":[]}");

        var request = new ExposureComputationServiceRequest(
            JobId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            HazardLayerId: Guid.NewGuid(),
            Mode: "BaselineExposure",
            RequestedAtUtc: DateTimeOffset.UtcNow,
            RequestedByUserId: Guid.NewGuid(),
            ComputationVersion: ExposureComputationContractVersion.Current,
            CrsPolicy: ExposureComputationCrsPolicy.RequireExplicitEpsg4326(),
            GeometryPolicy: ExposureComputationGeometryPolicy.FailFastNoRepair(),
            HazardLayer: new ExposureComputationHazardLayer(
                Id: Guid.NewGuid(),
                HazardType: "River",
                Severity: "High",
                MapAssetId: null),
            HazardFeatures: hazardFeatures,
            Barangays: Array.Empty<ExposureComputationBarangay>(),
            CriticalFacilities: Array.Empty<ExposureComputationCriticalFacility>());

        var errors = ExposureComputationPayloadValidation.ValidateRequest(request);
        Assert.Empty(errors);
    }
}

