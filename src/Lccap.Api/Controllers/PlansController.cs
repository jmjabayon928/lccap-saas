using System.Text.Json;
using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Maps.Commands;
using Lccap.Application.Maps.Queries;
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

    [HttpGet("{planId:guid}/operational-dashboard")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetOperationalDashboard(
        Guid planId,
        [FromQuery] int? recentActivityLimit,
        [FromServices] GetPlanOperationalDashboardQuery query,
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

        var limit = recentActivityLimit ?? 15;
        var result = await query.Execute(planId, limit, cancellationToken);
        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(result.Dashboard),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpGet("{planId:guid}/map-workspace")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetPlanMapWorkspace(
        Guid planId,
        [FromServices] GetPlanMapWorkspaceQuery query,
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
            200 when result.Workspace is not null => Ok(result.Workspace),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpPost("{planId:guid}/geojson-layers")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreateGeoJsonLayer(
        Guid planId,
        [FromBody] CreateGeoJsonLayerApiRequest body,
        [FromServices] CreateGeoJsonLayerCommand command,
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

        if (body is null || body.GeoJson is null)
        {
            return BadRequest(new { errors = new[] { "GeoJson is required." } });
        }

        var request = new CreateGeoJsonLayerRequest(
            body.FileAssetId,
            body.Name,
            body.MapType,
            body.Description,
            body.GeoJson,
            body.DefaultStyleJson,
            body.BoundsJson);

        var outcome = await command.Execute(planId, request, cancellationToken).ConfigureAwait(false);
        return outcome.StatusCode switch
        {
            201 when outcome.Summary is not null => StatusCode(StatusCodes.Status201Created, outcome.Summary),
            StatusCodes.Status400BadRequest => BadRequest(new { errors = outcome.Errors }),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(outcome.StatusCode),
        };
    }

    [HttpGet("/api/map-assets/{mapAssetId:guid}/features")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetGeoJsonLayerFeatures(
        Guid mapAssetId,
        [FromQuery] int? limit,
        [FromServices] GetGeoJsonLayerFeaturesQuery query,
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

        var result = await query.Execute(mapAssetId, limit ?? 500, cancellationToken).ConfigureAwait(false);
        return result.StatusCode switch
        {
            200 when result.Features is not null => Ok(new { items = result.Features }),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }

    [HttpDelete("/api/map-assets/{mapAssetId:guid}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ArchiveMapAsset(Guid mapAssetId, [FromServices] ArchiveMapAssetCommand command,
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

        var result = await command.Execute(mapAssetId, cancellationToken).ConfigureAwait(false);
        return result.StatusCode switch
        {
            StatusCodes.Status204NoContent => NoContent(),
            StatusCodes.Status403Forbidden => Forbid(),
            StatusCodes.Status404NotFound => NotFound(),
            _ => StatusCode(result.StatusCode),
        };
    }
}

public sealed record CreateGeoJsonLayerApiRequest(
    Guid FileAssetId,
    string Name,
    string MapType,
    string? Description,
    JsonDocument GeoJson,
    JsonDocument? DefaultStyleJson,
    JsonDocument? BoundsJson);

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
