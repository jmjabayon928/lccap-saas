using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Xunit;

namespace Lccap.Application.Tests.ExposureAnalysisJobs.Computation.Python;

public sealed class PythonExposureComputationClientAdapterTests
{
    private static ExposureComputationServiceRequest CreateDummyRequest() =>
        new(
            JobId: Guid.NewGuid(),
            AccountId: Guid.NewGuid(),
            PlanId: Guid.NewGuid(),
            HazardLayerId: Guid.NewGuid(),
            Mode: null,
            RequestedAtUtc: null,
            RequestedByUserId: null,
            ComputationVersion: "test",
            CrsPolicy: null,
            GeometryPolicy: null,
            HazardLayer: null,
            HazardFeatures: null,
            Barangays: null,
            CriticalFacilities: null);

    private static ExposureComputationDiagnostics CreateDiagnostics() =>
        new(
            Message: null,
            Warnings: Array.Empty<string>(),
            ValidationNotes: Array.Empty<string>(),
            GeometryFeatureCount: null,
            BarangayCount: null,
            CriticalFacilityCount: null,
            CrsDescription: null);

    private static ExposureComputationServiceResponse EngineUnavailableResponse(
        DateTimeOffset completedAtUtc,
        string engineName = "ExposureComputationScaffold",
        string? engineVersion = "scaffold") =>
        new(
            Success: false,
            EngineName: engineName,
            EngineVersion: engineVersion,
            ComputationRunId: null,
            CompletedAtUtc: completedAtUtc,
            ErrorCode: ExposureComputationEngineErrorCode.EngineUnavailable,
            ErrorMessage: "Exposure computation engine is not configured.",
            Diagnostics: CreateDiagnostics(),
            Results: Array.Empty<ExposureComputationServiceResultRow>());

    private static ExposureComputationServiceResponse ValidationFailedResponse(
        DateTimeOffset completedAtUtc,
        string engineName = "ExposureComputationScaffold",
        string? engineVersion = "scaffold") =>
        new(
            Success: false,
            EngineName: engineName,
            EngineVersion: engineVersion,
            ComputationRunId: null,
            CompletedAtUtc: completedAtUtc,
            ErrorCode: ExposureComputationEngineErrorCode.ValidationFailed,
            ErrorMessage: "Exposure computation service returned an invalid response.",
            Diagnostics: CreateDiagnostics(),
            Results: Array.Empty<ExposureComputationServiceResultRow>());

    private static ExposureComputationServiceResponse UnknownErrorResponse(
        DateTimeOffset completedAtUtc,
        string errorCode,
        string engineName = "ExposureComputationScaffold",
        string? engineVersion = "scaffold") =>
        new(
            Success: false,
            EngineName: engineName,
            EngineVersion: engineVersion,
            ComputationRunId: null,
            CompletedAtUtc: completedAtUtc,
            ErrorCode: errorCode,
            ErrorMessage: "some error",
            Diagnostics: CreateDiagnostics(),
            Results: Array.Empty<ExposureComputationServiceResultRow>());

    private static ExposureComputationServiceResponse SuccessResponse(
        DateTimeOffset completedAtUtc,
        string engineName = "ExposureComputationScaffold",
        string? engineVersion = "scaffold")
    {
        var hazardType = "Flood";
        var hazardLayerId = Guid.NewGuid();
        var criticalFacilityId = Guid.NewGuid();

        // SummaryJson exists to satisfy the result-row contract; adapter ignores Results for this slice.
        var summaryJson = JsonDocument.Parse("{}");

        var row = new ExposureComputationServiceResultRow(
            BarangayId: null,
            CriticalFacilityId: criticalFacilityId,
            HazardLayerId: hazardLayerId,
            HazardType: hazardType,
            Severity: "High",
            ExposedAreaHectares: null,
            ExposedFacilityCount: 1,
            ExposedPopulation: null,
            RiskScore: null,
            SummaryJson: summaryJson);

        return new ExposureComputationServiceResponse(
            Success: true,
            EngineName: engineName,
            EngineVersion: engineVersion,
            ComputationRunId: null,
            CompletedAtUtc: completedAtUtc,
            ErrorCode: null,
            ErrorMessage: null,
            Diagnostics: CreateDiagnostics(),
            Results: new[] { row });
    }

    [Fact]
    public async Task ExecuteAsync_maps_engine_unavailable_response_to_not_configured_failure()
    {
        var completedAtUtc = DateTimeOffset.UtcNow;
        var pythonResponse = EngineUnavailableResponse(completedAtUtc);

        var stubClient = new StubPythonClient(pythonResponse);
        var adapter = new PythonExposureComputationClientAdapter(stubClient);

        var result = await adapter.ExecuteAsync(CreateDummyRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Exposure computation engine is not configured.", result.ErrorMessage);
        Assert.Equal("ExposureComputationScaffold", result.EngineName);
        Assert.Equal("scaffold", result.EngineVersion);
        Assert.Equal(completedAtUtc, result.CompletedAtUtc);
        Assert.Equal(1, stubClient.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_maps_validation_failed_response_to_invalid_response_failure()
    {
        var completedAtUtc = DateTimeOffset.UtcNow;
        var pythonResponse = ValidationFailedResponse(completedAtUtc);

        var stubClient = new StubPythonClient(pythonResponse);
        var adapter = new PythonExposureComputationClientAdapter(stubClient);

        var result = await adapter.ExecuteAsync(CreateDummyRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Exposure computation service returned an invalid response.",
            result.ErrorMessage);
        Assert.Equal("ExposureComputationScaffold", result.EngineName);
        Assert.Equal("scaffold", result.EngineVersion);
        Assert.Equal(completedAtUtc, result.CompletedAtUtc);
        Assert.Equal(1, stubClient.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_maps_unknown_error_code_to_invalid_response_failure()
    {
        var completedAtUtc = DateTimeOffset.UtcNow;
        var pythonResponse = UnknownErrorResponse(completedAtUtc, errorCode: "UnexpectedCode");

        var stubClient = new StubPythonClient(pythonResponse);
        var adapter = new PythonExposureComputationClientAdapter(stubClient);

        var result = await adapter.ExecuteAsync(CreateDummyRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Exposure computation service returned an invalid response.",
            result.ErrorMessage);
        Assert.Equal("ExposureComputationScaffold", result.EngineName);
        Assert.Equal("scaffold", result.EngineVersion);
        Assert.Equal(completedAtUtc, result.CompletedAtUtc);
        Assert.Equal(1, stubClient.CallCount);
    }

    [Fact]
    public async Task ExecuteAsync_maps_success_response_to_success_without_persisting_results()
    {
        var completedAtUtc = DateTimeOffset.UtcNow;
        var pythonResponse = SuccessResponse(completedAtUtc);

        var stubClient = new StubPythonClient(pythonResponse);
        var adapter = new PythonExposureComputationClientAdapter(stubClient);

        var result = await adapter.ExecuteAsync(CreateDummyRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Equal("ExposureComputationScaffold", result.EngineName);
        Assert.Equal("scaffold", result.EngineVersion);
        Assert.Equal(completedAtUtc, result.CompletedAtUtc);
        Assert.Equal(1, stubClient.CallCount);
    }

    private sealed class StubPythonClient : IPythonExposureComputationServiceClient
    {
        private readonly ExposureComputationServiceResponse _response;

        public StubPythonClient(ExposureComputationServiceResponse response)
        {
            _response = response;
        }

        public int CallCount { get; private set; }

        public Task<ExposureComputationServiceResponse> ExecuteAsync(
            ExposureComputationServiceRequest request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_response);
        }
    }
}

