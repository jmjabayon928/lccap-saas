using Lccap.Application.Plans.Commands;
using Lccap.Application.Plans.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans")]
public sealed class PlansController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPlans([FromServices] GetPlansQuery query, CancellationToken cancellationToken)
    {
        var result = await query.Execute(cancellationToken);
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(new { plans = result.Plans }),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpPost]
    public async Task<IActionResult> CreatePlan([FromBody] CreatePlanApiRequest request, [FromServices] CreatePlanCommand command, CancellationToken cancellationToken)
    {
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
    public async Task<IActionResult> UpdatePlan(Guid planId, [FromBody] UpdatePlanApiRequest request, [FromServices] UpdatePlanCommand command, CancellationToken cancellationToken)
    {
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

    [HttpGet("{planId:guid}")]
    public async Task<IActionResult> GetPlanById(Guid planId, [FromServices] GetPlanByIdQuery query, CancellationToken cancellationToken)
    {
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
