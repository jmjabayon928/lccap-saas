using System.Security.Cryptography;
using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Monitoring;
using Lccap.Application.Monitoring.Commands;
using Lccap.Application.Monitoring.Queries;
using Lccap.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public MonitoringController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpPost("indicators")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreateIndicator(
        [FromBody] CreateIndicatorRequest request,
        [FromServices] CreateIndicatorCommand command,
        [FromServices] ILccapDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Indicator name must not be blank.");
        }

        if (!IsValidStatus(request.Status))
        {
            return BadRequest("Indicator status is invalid.");
        }

        if (request.ProgressPercent is < 0m or > 100m)
        {
            return BadRequest("Progress percent must be between 0 and 100 when provided.");
        }

        var accountId = _currentUser.AccountId;
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

        var mergedMetadata = MonitoringIndicatorMetadataHelper.Merge(
            normalized.MetadataJson,
            request.CurrentValue,
            request.ProgressPercent,
            request.Frequency,
            request.ResponsibleOffice);

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
            MetadataJson = mergedMetadata,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            IsDeleted = false
        };
        indicator.EnsureRowVersion();

        _ = dbContext.MonitoringIndicators.Add(indicator);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToIndicatorResponse(indicator));
    }

    [HttpPut("indicators/{indicatorId:guid}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> UpdateIndicator(
        Guid indicatorId,
        [FromBody] UpdateIndicatorRequest request,
        [FromServices] UpdateMonitoringIndicatorCommand command,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Indicator name must not be blank.");
        }

        if (!IsValidStatus(request.Status))
        {
            return BadRequest("Indicator status is invalid.");
        }

        var rowVersionText = !string.IsNullOrWhiteSpace(request.RowVersion)
            ? request.RowVersion
            : request.RowVersionBase64;
        if (string.IsNullOrWhiteSpace(rowVersionText))
        {
            return BadRequest("Row version is required.");
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(rowVersionText);
        }
        catch (FormatException)
        {
            return BadRequest("Row version is invalid.");
        }

        if (_currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return Unauthorized("Authenticated account context is required.");
        }

        var outcome = await command.ExecuteAsync(
            new UpdateMonitoringIndicatorCommand.Request(
                indicatorId,
                request.Name,
                request.Description,
                request.Unit,
                request.BaselineValue,
                request.TargetValue,
                request.CurrentValue,
                request.ProgressPercent,
                request.Frequency,
                request.ResponsibleOffice,
                request.Status,
                rowVersion),
            cancellationToken);

        return outcome.Outcome switch
        {
            UpdateMonitoringIndicatorCommand.Outcome.Success when outcome.Indicator is not null =>
                Ok(ToIndicatorResponse(outcome.Indicator)),
            UpdateMonitoringIndicatorCommand.Outcome.NotFound => NotFound("Indicator not found for the current account."),
            UpdateMonitoringIndicatorCommand.Outcome.Unauthorized =>
                Unauthorized("Authenticated account context is required."),
            UpdateMonitoringIndicatorCommand.Outcome.Concurrency =>
                BadRequest("Indicator was updated by another request."),
            UpdateMonitoringIndicatorCommand.Outcome.ValidationFailed =>
                BadRequest(outcome.ValidationMessage ?? "Validation failed."),
            _ => BadRequest("Could not update indicator."),
        };
    }

    [HttpDelete("indicators/{indicatorId:guid}")]
    [RequireWorkspaceRole("Archive")]
    public async Task<IActionResult> ArchiveIndicator(
        Guid indicatorId,
        [FromServices] ArchiveMonitoringIndicatorCommand command,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanArchive(_currentUser.Role))
        {
            return Forbid();
        }

        var outcome = await command.ExecuteAsync(indicatorId, cancellationToken);
        return outcome.Outcome switch
        {
            ArchiveMonitoringIndicatorCommand.Outcome.Success => NoContent(),
            ArchiveMonitoringIndicatorCommand.Outcome.NotFound => NotFound("Indicator not found for the current account."),
            ArchiveMonitoringIndicatorCommand.Outcome.Unauthorized =>
                Unauthorized("Authenticated account context is required."),
            _ => NotFound("Indicator not found for the current account."),
        };
    }

    [HttpGet("plans/{planId:guid}/indicators")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetIndicators(
        Guid planId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] GetIndicatorsByPlanQuery query,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var paged = await query.ExecuteAsync(planId, page, pageSize, cancellationToken);
        // Note: rowVersion repair is handled inside query for the page slice.
        return Ok(new
        {
            items = paged.Items.Select(ToIndicatorResponse).ToList(),
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount
        });
    }

    private static bool IsValidStatus(string status)
    {
        return status is "NotStarted" or "InProgress" or "OnTrack" or "Delayed" or "Completed";
    }

    private static IndicatorResponse ToIndicatorResponse(MonitoringIndicator i)
    {
        var m = MonitoringIndicatorMetadataHelper.Parse(i.MetadataJson);
        return new IndicatorResponse(
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
            m.CurrentValue,
            m.ProgressPercent,
            m.Frequency,
            m.ResponsibleOffice,
            i.CreatedAtUtc,
            i.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(i.RowVersion));
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
        string? MetadataJson,
        decimal? CurrentValue = null,
        decimal? ProgressPercent = null,
        string? Frequency = null,
        string? ResponsibleOffice = null);

    public sealed record UpdateIndicatorRequest(
        string Name,
        string? Description,
        decimal? BaselineValue,
        decimal? TargetValue,
        string? Unit,
        string Status,
        decimal? CurrentValue = null,
        decimal? ProgressPercent = null,
        string? Frequency = null,
        string? ResponsibleOffice = null,
        string? RowVersion = null,
        string? RowVersionBase64 = null);

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
        decimal? CurrentValue,
        decimal? ProgressPercent,
        string? Frequency,
        string? ResponsibleOffice,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? UpdatedAtUtc,
        string RowVersion);
}
