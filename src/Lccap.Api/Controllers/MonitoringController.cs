using Lccap.Application.Monitoring.Commands;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring.Queries;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    [HttpPost("indicators")]
    public async Task<IActionResult> CreateIndicator(
        [FromBody] CreateIndicatorRequest request,
        [FromServices] CreateIndicatorCommand command,
        [FromServices] LccapDbContext dbContext,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Indicator name must not be blank.");
        }

        if (!IsValidStatus(request.Status))
        {
            return BadRequest("Indicator status is invalid.");
        }

        var accountId = currentUser.AccountId;
        if (accountId is null)
        {
            return Unauthorized("Authenticated account context is required.");
        }

        var normalized = command.Execute(
            new CreateIndicatorCommand.Request(
                accountId.Value,
                request.PlanId,
                request.ActionItemId,
                request.Name,
                request.Description,
                request.BaselineValue,
                request.TargetValue,
                request.Unit,
                request.Status,
                request.MetadataJson ?? "{}"));

        var planExists = await dbContext.Plans
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == request.PlanId
                    && p.AccountId == accountId.Value
                    && !p.IsDeleted,
                cancellationToken);

        if (!planExists)
        {
            return NotFound("Plan not found for the current account.");
        }

        var indicator = new MonitoringIndicator
        {
            AccountId = normalized.AccountId,
            PlanId = normalized.PlanId,
            ActionItemId = normalized.ActionItemId,
            Name = normalized.Name,
            Description = normalized.Description,
            BaselineValue = normalized.BaselineValue,
            TargetValue = normalized.TargetValue,
            Unit = normalized.Unit,
            Status = normalized.Status,
            MetadataJson = normalized.MetadataJson,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId,
            IsDeleted = false
        };

        _ = dbContext.MonitoringIndicators.Add(indicator);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new { indicator.Id, RowVersion = Convert.ToBase64String(indicator.RowVersion) });
    }

    [HttpPut("indicators/{indicatorId:guid}")]
    public async Task<IActionResult> UpdateIndicator(
        Guid indicatorId,
        [FromBody] UpdateIndicatorRequest request,
        [FromServices] UpdateIndicatorCommand command,
        [FromServices] LccapDbContext dbContext,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Indicator name must not be blank.");
        }

        if (!IsValidStatus(request.Status))
        {
            return BadRequest("Indicator status is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.RowVersionBase64))
        {
            return BadRequest("Row version is required.");
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersionBase64);
        }
        catch (FormatException)
        {
            return BadRequest("Row version is invalid.");
        }

        var accountId = currentUser.AccountId;
        if (accountId is null)
        {
            return Unauthorized("Authenticated account context is required.");
        }

        var normalized = command.Execute(
            new UpdateIndicatorCommand.Request(
                accountId.Value,
                indicatorId,
                request.Name,
                request.Description,
                request.BaselineValue,
                request.TargetValue,
                request.Unit,
                request.Status,
                currentUser.UserId,
                rowVersion));

        var indicator = await dbContext.MonitoringIndicators
            .FirstOrDefaultAsync(
                i => i.Id == indicatorId
                    && i.AccountId == accountId.Value
                    && !i.IsDeleted,
                cancellationToken);

        if (indicator is null)
        {
            return NotFound("Indicator not found for the current account.");
        }

        dbContext.Entry(indicator).Property(i => i.RowVersion).OriginalValue = normalized.RowVersion;

        indicator.UpdateDefinition(
            normalized.Name,
            normalized.Description,
            normalized.BaselineValue,
            normalized.TargetValue,
            normalized.Unit,
            normalized.Status,
            normalized.UserId,
            DateTimeOffset.UtcNow);

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BadRequest("Indicator was updated by another request.");
        }

        return Ok(new { RowVersion = Convert.ToBase64String(indicator.RowVersion) });
    }

    [HttpGet("plans/{planId:guid}/indicators")]
    public async Task<IActionResult> GetIndicators(
        Guid planId,
        [FromServices] LccapDbContext dbContext,
        [FromServices] ICurrentUserContext currentUser,
        CancellationToken cancellationToken)
    {
        var accountId = currentUser.AccountId;
        if (accountId is null)
        {
            return Unauthorized("Authenticated account context is required.");
        }

        var planExists = await dbContext.Plans
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == planId
                    && p.AccountId == accountId.Value
                    && !p.IsDeleted,
                cancellationToken);

        if (!planExists)
        {
            return NotFound("Plan not found for the current account.");
        }

        var results = await dbContext.MonitoringIndicators
            .AsNoTracking()
            .Where(i => i.AccountId == accountId.Value && i.PlanId == planId && !i.IsDeleted)
            .OrderBy(i => i.Name)
            .Select(i => new GetIndicatorsQuery.Result(
                i.Id,
                i.AccountId,
                i.PlanId,
                i.ActionItemId,
                i.Name,
                i.Description,
                i.BaselineValue,
                i.TargetValue,
                i.Unit,
                i.Status,
                i.MetadataJson,
                i.CreatedAtUtc,
                i.UpdatedAtUtc,
                i.RowVersion))
            .ToListAsync(cancellationToken);

        return Ok(results.Select(r => new IndicatorResponse(
            r.Id,
            r.AccountId,
            r.PlanId,
            r.ActionItemId,
            r.Name,
            r.Description,
            r.BaselineValue,
            r.TargetValue,
            r.Unit,
            r.Status,
            r.MetadataJson,
            r.CreatedAtUtc,
            r.UpdatedAtUtc,
            Convert.ToBase64String(r.RowVersion))));
    }

    private static bool IsValidStatus(string status)
    {
        return status is "NotStarted" or "InProgress" or "OnTrack" or "Delayed" or "Completed";
    }

    public sealed record CreateIndicatorRequest(
        Guid PlanId,
        Guid? ActionItemId,
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        string? MetadataJson);

    public sealed record UpdateIndicatorRequest(
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        string RowVersionBase64);

    public sealed record IndicatorResponse(
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
        string RowVersionBase64);
}
