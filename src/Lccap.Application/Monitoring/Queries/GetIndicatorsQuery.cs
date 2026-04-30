namespace Lccap.Application.Monitoring.Queries;

public sealed class GetIndicatorsQuery
{
    public sealed record Result(
        Guid Id,
        Guid AccountId,
        Guid PlanId,
        Guid? ActionItemId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        string MetadataJson,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc,
        byte[] RowVersion);
}
