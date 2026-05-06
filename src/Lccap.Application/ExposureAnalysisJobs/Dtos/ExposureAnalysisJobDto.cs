namespace Lccap.Application.ExposureAnalysisJobs.Dtos;

public sealed record ExposureAnalysisJobDto(
    Guid Id,
    Guid PlanId,
    string Status,
    Guid HazardLayerId,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc);

