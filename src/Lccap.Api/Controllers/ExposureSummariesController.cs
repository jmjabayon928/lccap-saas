using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureSummaries.Dtos;
using Lccap.Application.ExposureSummaries.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class ExposureSummariesController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public ExposureSummariesController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("{planId:guid}/exposure-summaries")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlanExposureSummaries(
        Guid planId,
        [FromServices] GetPlanExposureSummariesQuery query,
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
            _ => StatusCode(result.StatusCode)
        };
    }

    [HttpGet("{planId:guid}/exposure-analysis-jobs/{jobId:guid}/exposure-summaries")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetJobExposureSummaries(
        Guid planId,
        Guid jobId,
        [FromServices] GetJobExposureSummariesQuery query,
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
            200 when result.Items is not null => Ok(new { items = result.Items }),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            _ => StatusCode(result.StatusCode)
        };
    }

    [HttpGet("{planId:guid}/exposure-summaries/{summaryId:guid}")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetExposureSummary(
        Guid planId,
        Guid summaryId,
        [FromServices] GetExposureSummaryQuery query,
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

        var result = await query.Execute(planId, summaryId, cancellationToken).ConfigureAwait(false);
        return result.StatusCode switch
        {
            200 when result.Summary is not null => Ok(result.Summary),
            401 => Unauthorized(),
            403 => Forbid(),
            404 => NotFound(),
            _ => StatusCode(result.StatusCode)
        };
    }
}

