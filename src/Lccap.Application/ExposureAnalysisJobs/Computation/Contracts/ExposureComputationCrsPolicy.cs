namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationCrsPolicy(
    string SourceType,
    int? SourceEpsg,
    string TargetType,
    int? TargetEpsg,
    bool FailOnAmbiguity)
{
    public static ExposureComputationCrsPolicy RequireExplicitEpsg4326()
    {
        return new ExposureComputationCrsPolicy(
            SourceType: "Explicit",
            SourceEpsg: 4326,
            TargetType: "Explicit",
            TargetEpsg: 4326,
            FailOnAmbiguity: true);
    }
}

