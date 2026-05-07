using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

namespace Lccap.Application.ExposureAnalysisJobs.Computation;

public sealed record ExposureComputationResult(
    bool IsSuccess,
    string? ErrorMessage,
    string EngineName,
    string? EngineVersion,
    DateTimeOffset CompletedAtUtc,
    string? ComputationRunId,
    ExposureComputationDiagnostics Diagnostics,
    IReadOnlyList<ExposureComputationServiceResultRow> Results)
{
    private static ExposureComputationDiagnostics CreateDefaultDiagnostics(string? message)
    {
        return new ExposureComputationDiagnostics(
            Message: message ?? string.Empty,
            Warnings: Array.Empty<string>(),
            ValidationNotes: Array.Empty<string>(),
            GeometryFeatureCount: null,
            BarangayCount: null,
            CriticalFacilityCount: null,
            CrsDescription: null);
    }

    private static IReadOnlyList<ExposureComputationServiceResultRow> EmptyResults() =>
        Array.Empty<ExposureComputationServiceResultRow>();

    public static ExposureComputationResult Failed(
        string errorMessage,
        string engineName,
        string? engineVersion,
        DateTimeOffset completedAtUtc,
        string? computationRunId = null,
        ExposureComputationDiagnostics? diagnostics = null,
        IReadOnlyList<ExposureComputationServiceResultRow>? results = null)
    {
        return new ExposureComputationResult(
            IsSuccess: false,
            ErrorMessage: errorMessage,
            EngineName: engineName,
            EngineVersion: engineVersion,
            CompletedAtUtc: completedAtUtc,
            ComputationRunId: computationRunId,
            Diagnostics: diagnostics ?? CreateDefaultDiagnostics(errorMessage),
            Results: results ?? EmptyResults());
    }

    public static ExposureComputationResult Succeeded(
        string? engineVersion,
        DateTimeOffset completedAtUtc,
        string? computationRunId = null,
        ExposureComputationDiagnostics? diagnostics = null,
        IReadOnlyList<ExposureComputationServiceResultRow>? results = null)
    {
        return new ExposureComputationResult(
            IsSuccess: true,
            ErrorMessage: null,
            EngineName: "Success",
            EngineVersion: engineVersion,
            CompletedAtUtc: completedAtUtc,
            ComputationRunId: computationRunId,
            Diagnostics: diagnostics ?? CreateDefaultDiagnostics(message: string.Empty),
            Results: results ?? EmptyResults());
    }
}

