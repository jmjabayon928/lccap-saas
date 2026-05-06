using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.HazardLayers.Commands;
using Lccap.Application.HazardLayers.Dtos;
using Lccap.Application.HazardLayers.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class HazardLayersController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public HazardLayersController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("{planId:guid}/hazard-layers")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlanHazardLayers(
        Guid planId,
        [FromServices] GetPlanHazardLayersQuery query,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null)
        {
            return Forbid();
        }

        var result = await query.Execute(planId, cancellationToken).ConfigureAwait(false);
        return result.StatusCode switch
        {
            200 when result.Items is not null => Ok(new { items = result.Items }),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpPost("{planId:guid}/hazard-layers")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> RegisterHazardLayer(
        Guid planId,
        [FromBody] RegisterHazardLayerRequest request,
        [FromServices] RegisterHazardLayerCommand command,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(new { errors = new[] { "Request is required." } });
        }

        var outcome = await command.Execute(planId, request, cancellationToken).ConfigureAwait(false);
        return outcome.StatusCode switch
        {
            201 when outcome.HazardLayer is not null => StatusCode(StatusCodes.Status201Created, outcome.HazardLayer),
            400 => BadRequest(new { errors = outcome.Errors }),
            403 => Forbid(),
            404 => NotFound(),
            409 => Conflict(),
            _ => StatusCode(outcome.StatusCode),
        };
    }
}

