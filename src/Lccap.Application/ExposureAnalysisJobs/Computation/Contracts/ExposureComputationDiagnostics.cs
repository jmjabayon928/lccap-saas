namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationDiagnostics(
    string? Message,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> ValidationNotes,
    int? GeometryFeatureCount,
    int? BarangayCount,
    int? CriticalFacilityCount,
    string? CrsDescription);

