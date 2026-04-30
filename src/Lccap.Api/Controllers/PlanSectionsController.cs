using Lccap.Application.Sections.Commands;
using Lccap.Application.Sections.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/sections")]
public sealed class PlanSectionsController : ControllerBase
{
    private readonly SavePlanSectionCommand _savePlanSectionCommand;
    private readonly GetPlanSectionsQuery _getPlanSectionsQuery;
    private readonly GetPlanSectionByKeyQuery _getPlanSectionByKeyQuery;

    public PlanSectionsController(
        SavePlanSectionCommand savePlanSectionCommand,
        GetPlanSectionsQuery getPlanSectionsQuery,
        GetPlanSectionByKeyQuery getPlanSectionByKeyQuery)
    {
        _savePlanSectionCommand = savePlanSectionCommand;
        _getPlanSectionsQuery = getPlanSectionsQuery;
        _getPlanSectionByKeyQuery = getPlanSectionByKeyQuery;
    }

    [HttpGet]
    public async Task<IActionResult> GetSections(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _getPlanSectionsQuery.ExecuteAsync(planId, cancellationToken);
        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return Ok(result.Sections);
    }

    [HttpGet("{sectionKey}")]
    public async Task<IActionResult> GetByKey(Guid planId, string sectionKey, CancellationToken cancellationToken)
    {
        var result = await _getPlanSectionByKeyQuery.ExecuteAsync(planId, sectionKey, cancellationToken);
        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return Ok(result.Section);
    }

    [HttpPut("{sectionKey}")]
    public async Task<IActionResult> Save(Guid planId, string sectionKey, [FromBody] SavePlanSectionBody request, CancellationToken cancellationToken)
    {
        var result = await _savePlanSectionCommand.ExecuteAsync(
            new SavePlanSectionRequest(planId, sectionKey, request.Title, request.Content, request.SortOrder),
            cancellationToken);

        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(
            new SavePlanSectionResponse(
                result.PlanSectionId!.Value,
                result.LastEditedByUserId,
                result.LastEditedAtUtc));
    }
}

public sealed record SavePlanSectionBody(string Title, string Content, int SortOrder);

public sealed record SavePlanSectionResponse(Guid SectionId, Guid? LastEditedByUserId, DateTimeOffset? LastEditedAtUtc);
