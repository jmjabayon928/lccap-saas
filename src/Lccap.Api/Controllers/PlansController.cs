using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Plans.Commands;
using Lccap.Application.Plans.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlansController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public PlansController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlans(
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromServices] GetPlansQuery query,
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

        var paged = await query.Execute(page, pageSize, cancellationToken);
        return Ok(new
        {
            items = paged.Items,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount
        });
    }

    [HttpPost]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanApiRequest request, [FromServices] CreatePlanCommand command, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await command.Execute(
            new CreatePlanRequest(
                request.Title,
                request.StartYear,
                request.EndYear,
                request.Status,
                request.TemplateMode,
                request.VersionNumber,
                request.Description,
                request.SubmittedAtUtc,
                request.ApprovedAtUtc),
            cancellationToken);

        return result.StatusCode switch
        {
            StatusCodes.Status201Created => CreatedAtAction(nameof(GetPlanById), new { planId = result.Plan!.Id }, result.Plan),
            StatusCodes.Status400BadRequest => BadRequest(new { errors = result.Errors }),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpPut("{planId:guid}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanApiRequest request, [FromServices] UpdatePlanCommand command, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await command.Execute(
            planId,
            new UpdatePlanRequest(
                request.Title,
                request.StartYear,
                request.EndYear,
                request.Status,
                request.TemplateMode,
                request.VersionNumber,
                request.Description,
                request.SubmittedAtUtc,
                request.ApprovedAtUtc,
                request.RowVersion),
            cancellationToken);

        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(result.Plan),
            StatusCodes.Status400BadRequest => BadRequest(new { errors = result.Errors }),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            StatusCodes.Status409Conflict => Conflict(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpDelete("{planId:guid}")]
    [RequireWorkspaceRole("Archive")]
    public async Task<IActionResult> ArchivePlan(Guid planId, [FromServices] ArchivePlanCommand command, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanArchive(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await command.Execute(planId, cancellationToken);
        return result.StatusCode switch
        {
            StatusCodes.Status204NoContent => NoContent(),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpGet("{planId:guid}")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlanById(Guid planId, [FromServices] GetPlanByIdQuery query, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await query.Execute(planId, cancellationToken);
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(result.Plan),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }
}

public sealed record CreatePlanApiRequest(
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc);

public sealed record UpdatePlanApiRequest(
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    byte[] RowVersion);
