using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Commands;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Application.ExposureAnalysisJobs.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class ExposureAnalysisJobsController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public ExposureAnalysisJobsController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("{planId:guid}/exposure-analysis-jobs")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlanExposureAnalysisJobs(
        Guid planId,
        [FromServices] GetPlanExposureAnalysisJobsQuery query,
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

    [HttpGet("{planId:guid}/exposure-analysis-jobs/{jobId:guid}")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetExposureAnalysisJob(
        Guid planId,
        Guid jobId,
        [FromServices] GetExposureAnalysisJobQuery query,
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

        var result = await query.Execute(planId, jobId, cancellationToken).ConfigureAwait(false);
        return result.StatusCode switch
        {
            200 when result.Job is not null => Ok(result.Job),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpPost("{planId:guid}/exposure-analysis-jobs")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreateExposureAnalysisJob(
        Guid planId,
        [FromBody] CreateExposureAnalysisJobRequest request,
        [FromServices] CreateExposureAnalysisJobCommand command,
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
            201 when outcome.Job is not null => StatusCode(StatusCodes.Status201Created, outcome.Job),
            400 => BadRequest(new { errors = outcome.Errors }),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            409 => Conflict(),
            _ => StatusCode(outcome.StatusCode),
        };
    }

    [HttpPost("{planId:guid}/exposure-analysis-jobs/{jobId:guid}/process")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ProcessExposureAnalysisJob(
        Guid planId,
        Guid jobId,
        [FromServices] ProcessExposureAnalysisJobCommand command,
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

        var outcome = await command.Execute(planId, jobId, cancellationToken).ConfigureAwait(false);
        return outcome.StatusCode switch
        {
            200 when outcome.Job is not null => Ok(outcome.Job),
            400 => BadRequest(new { errors = outcome.Errors }),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            409 => Conflict(),
            _ => StatusCode(outcome.StatusCode),
        };
    }
}

