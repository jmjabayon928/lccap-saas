namespace Lccap.Application.ExposureAnalysisJobs.Computation;

public sealed record ExposureComputationResult(
    bool IsSuccess,
    string? ErrorMessage,
    string EngineName,
    string? EngineVersion,
    DateTimeOffset CompletedAtUtc)
{
    public static ExposureComputationResult Failed(
        string errorMessage,
        string engineName,
        string? engineVersion,
        DateTimeOffset completedAtUtc)
    {
        return new ExposureComputationResult(
            IsSuccess: false,
            ErrorMessage: errorMessage,
            EngineName: engineName,
            EngineVersion: engineVersion,
            CompletedAtUtc: completedAtUtc);
    }

    public static ExposureComputationResult Succeeded(
        string? engineVersion,
        DateTimeOffset completedAtUtc)
    {
        return new ExposureComputationResult(
            IsSuccess: true,
            ErrorMessage: null,
            EngineName: "Success",
            EngineVersion: engineVersion,
            CompletedAtUtc: completedAtUtc);
    }
}

