namespace Lccap.Application.ExposureAnalysisJobs.Computation;

public sealed record ExposureComputationRequest(
    Guid JobId,
    Guid AccountId,
    Guid PlanId,
    Guid? HazardLayerId,
    DateTimeOffset? RequestedAtUtc,
    Guid? RequestedByUserId,
    string? Mode);

