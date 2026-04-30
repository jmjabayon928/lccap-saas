using System.Text.Json;
using Lccap.Application.Actions.Commands;
using Lccap.Application.Actions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class ActionItemsController : ControllerBase
{
    [HttpPost("plans/{planId:guid}/actions")]
    public async Task<IActionResult> Create(
        Guid planId,
        [FromBody] CreateActionItemApiRequest body,
        [FromServices] CreateActionItemCommand command,
        CancellationToken cancellationToken)
    {
        JsonDocument? metadata = null;
        if (!string.IsNullOrWhiteSpace(body.MetadataJson))
        {
            metadata = JsonDocument.Parse(body.MetadataJson);
        }

        var request = new CreateActionItemRequest(
            body.Title,
            body.Description,
            body.ActionType,
            body.Sector,
            body.ResponsibleOffice,
            body.BudgetAmount,
            body.FundingSource,
            body.TimelineStartUtc,
            body.TimelineEndUtc,
            body.Kpi,
            body.PriorityScore,
            body.Status,
            metadata);

        var outcome = await command.ExecuteAsync(planId, request, cancellationToken);
        if (outcome.ForbiddenAccess)
        {
            return Forbid();
        }

        if (outcome.MissingPlan)
        {
            return NotFound();
        }

        if (!outcome.IsSuccess)
        {
            return BadRequest(new { errors = outcome.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { actionItemId = outcome.Item!.Id }, outcome.Item);
    }

    [HttpPut("actions/{actionItemId:guid}")]
    public async Task<IActionResult> Update(
        Guid actionItemId,
        [FromBody] UpdateActionItemApiRequest body,
        [FromServices] UpdateActionItemCommand command,
        CancellationToken cancellationToken)
    {
        JsonDocument? updateMetadata = null;
        if (!string.IsNullOrWhiteSpace(body.MetadataJson))
        {
            updateMetadata = JsonDocument.Parse(body.MetadataJson);
        }

        var request = new UpdateActionItemRequest(
            body.Title,
            body.Description,
            body.ActionType,
            body.Sector,
            body.ResponsibleOffice,
            body.BudgetAmount,
            body.FundingSource,
            body.TimelineStartUtc,
            body.TimelineEndUtc,
            body.Kpi,
            body.PriorityScore,
            body.Status,
            updateMetadata,
            body.RowVersion);

        var outcome = await command.ExecuteAsync(actionItemId, request, cancellationToken);
        if (outcome.ForbiddenAccess)
        {
            return Forbid();
        }

        if (outcome.MissingItem)
        {
            return NotFound();
        }

        if (outcome.ConcurrencyStale)
        {
            return Conflict();
        }

        if (!outcome.IsSuccess)
        {
            return BadRequest(new { errors = outcome.Errors });
        }

        return Ok(outcome.Item);
    }

    [HttpGet("plans/{planId:guid}/actions")]
    public async Task<IActionResult> ListForPlan(
        Guid planId,
        [FromServices] GetActionItemsByPlanQuery query,
        CancellationToken cancellationToken)
    {
        var outcome = await query.ExecuteAsync(planId, cancellationToken);
        if (outcome.ForbiddenAccess)
        {
            return Forbid();
        }

        if (outcome.MissingPlan)
        {
            return NotFound();
        }

        return Ok(outcome.Items);
    }

    [HttpGet("actions/{actionItemId:guid}")]
    [ActionName(nameof(GetById))]
    public async Task<IActionResult> GetById(
        Guid actionItemId,
        [FromServices] GetActionItemByIdQuery query,
        CancellationToken cancellationToken)
    {
        var outcome = await query.ExecuteAsync(actionItemId, cancellationToken);
        if (outcome.ForbiddenAccess)
        {
            return Forbid();
        }

        if (!outcome.IsSuccess)
        {
            return NotFound();
        }

        return Ok(outcome.Item);
    }
}

public sealed record CreateActionItemApiRequest(
    string Title,
    string? Description,
    string ActionType,
    string Sector,
    string? ResponsibleOffice,
    decimal? BudgetAmount,
    string? FundingSource,
    DateTimeOffset? TimelineStartUtc,
    DateTimeOffset? TimelineEndUtc,
    string? Kpi,
    decimal? PriorityScore,
    string? Status,
    string? MetadataJson);

public sealed record UpdateActionItemApiRequest(
    string Title,
    string? Description,
    string ActionType,
    string Sector,
    string? ResponsibleOffice,
    decimal? BudgetAmount,
    string? FundingSource,
    DateTimeOffset? TimelineStartUtc,
    DateTimeOffset? TimelineEndUtc,
    string? Kpi,
    decimal? PriorityScore,
    string? Status,
    string? MetadataJson,
    byte[] RowVersion);
