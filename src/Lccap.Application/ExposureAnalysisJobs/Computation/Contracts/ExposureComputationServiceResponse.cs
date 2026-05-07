using System.Text.Json;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationServiceResponse(
    bool Success,
    string EngineName,
    string? EngineVersion,
    string? ComputationRunId,
    DateTimeOffset CompletedAtUtc,
    string? ErrorCode,
    string? ErrorMessage,
    ExposureComputationDiagnostics Diagnostics,
    IReadOnlyList<ExposureComputationServiceResultRow> Results);

