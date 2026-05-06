using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Funding.Commands;
using Lccap.Application.Funding.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class FundingController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public FundingController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("funding/climate-expenditure-tags")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetClimateExpenditureTags(
        [FromServices] GetClimateExpenditureTagsQuery query,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await query.ExecuteAsync(includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("funding/sources")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetFundingSources(
        [FromServices] GetFundingSourcesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await query.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("funding/programs")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetFundingPrograms(
        [FromServices] GetFundingProgramsQuery query,
        [FromQuery] Guid? fundingSourceId = null,
        [FromQuery] bool includeInactiveOrClosed = false,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var outcome = await query.ExecuteAsync(fundingSourceId, includeInactiveOrClosed, cancellationToken);
        if (outcome.SourceNotFound || outcome.Result is null)
        {
            return NotFound();
        }

        return Ok(outcome.Result);
    }

    [HttpGet("plans/{planId:guid}/funding-allocations")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetAllocationsByPlan(
        Guid planId,
        [FromServices] GetActionFundingAllocationsByPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var outcome = await query.ExecuteAsync(planId, cancellationToken);
        if (!outcome.Ok || outcome.Result is null)
        {
            if (outcome.IsPlanMissing)
            {
                return NotFound();
            }

            return Ok(new GetActionFundingAllocationsListResult(Array.Empty<ActionFundingAllocationListItemDto>()));
        }

        return Ok(outcome.Result);
    }

    [HttpGet("actions/{actionItemId:guid}/funding-allocations")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetAllocationsByActionItem(
        Guid actionItemId,
        [FromServices] GetActionFundingAllocationsByActionQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var outcome = await query.ExecuteAsync(actionItemId, cancellationToken);
        if (!outcome.Ok || outcome.Result is null)
        {
            if (outcome.IsActionMissing)
            {
                return NotFound();
            }

            return Ok(new GetActionFundingAllocationsListResult(Array.Empty<ActionFundingAllocationListItemDto>()));
        }

        return Ok(outcome.Result);
    }

    [HttpPost("plans/{planId:guid}/funding-allocations")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreateAllocation(
        Guid planId,
        [FromBody] CreateActionFundingAllocationApiRequest body,
        [FromServices] CreateActionFundingAllocationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var request = new CreateActionFundingAllocationRequest(
            body.ActionItemId,
            body.FundingSourceId,
            body.FundingProgramId,
            body.ClimateExpenditureTagId,
            body.FiscalYear,
            body.AllocatedAmount,
            body.CurrencyCode,
            body.AllocationStatus,
            body.Notes);

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

        return StatusCode(StatusCodes.Status201Created, outcome.Dto);
    }

    [HttpDelete("funding-allocations/{allocationId:guid}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ArchiveAllocation(
        Guid allocationId,
        [FromServices] ArchiveActionFundingAllocationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await command.ExecuteAsync(allocationId, cancellationToken);
        if (result.ForbiddenAccount)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return NoContent();
    }
}

public sealed record CreateActionFundingAllocationApiRequest(
    Guid ActionItemId,
    Guid FundingSourceId,
    Guid? FundingProgramId,
    Guid? ClimateExpenditureTagId,
    int FiscalYear,
    decimal AllocatedAmount,
    string? CurrencyCode,
    string? AllocationStatus,
    string? Notes);
