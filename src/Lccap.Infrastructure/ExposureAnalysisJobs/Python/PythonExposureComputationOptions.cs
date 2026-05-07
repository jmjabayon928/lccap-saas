namespace Lccap.Infrastructure.ExposureAnalysisJobs.Python;

public sealed class PythonExposureComputationOptions
{
    public string? BaseUrl { get; init; }

    public int TimeoutSeconds { get; init; } = 30;

    public string ComputePath { get; init; } = "/compute/exposure";
}

