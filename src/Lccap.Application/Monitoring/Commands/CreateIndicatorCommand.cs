using Lccap.Domain.Entities;

namespace Lccap.Application.Monitoring.Commands;

public sealed class CreateIndicatorCommand
{
    public sealed record Request(
        Guid AccountId,
        Guid PlanId,
        Guid? ActionItemId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        string MetadataJson);

    public sealed record Result(
        Guid AccountId,
        Guid PlanId,
        Guid? ActionItemId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        string MetadataJson);

    public Result Execute(Request request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Indicator name must not be blank.", nameof(request.Name));
        }

        if (!MonitoringIndicator.IsValidStatus(request.Status))
        {
            throw new ArgumentException("Indicator status is invalid.", nameof(request.Status));
        }

        return new Result(
            request.AccountId,
            request.PlanId,
            request.ActionItemId,
            request.Name.Trim(),
            request.Description,
            request.BaselineValue,
            request.TargetValue,
            request.Unit,
            request.Status,
            string.IsNullOrWhiteSpace(request.MetadataJson) ? "{}" : request.MetadataJson);
    }
}
