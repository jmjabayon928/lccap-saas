using Lccap.Domain.Entities;

namespace Lccap.Application.Monitoring.Commands;

public sealed class UpdateIndicatorCommand
{
    public sealed record Request(
        Guid AccountId,
        Guid IndicatorId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        Guid? UserId,
        byte[] RowVersion);

    public sealed record Result(
        Guid AccountId,
        Guid IndicatorId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        Guid? UserId,
        byte[] RowVersion);

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
            request.IndicatorId,
            request.Name.Trim(),
            request.Description,
            request.BaselineValue,
            request.TargetValue,
            request.Unit,
            request.Status,
            request.UserId,
            request.RowVersion);
    }
}
