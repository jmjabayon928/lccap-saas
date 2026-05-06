using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
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
    private readonly GetPlanSectionHistoryQuery _getPlanSectionHistoryQuery;
    private readonly RestorePlanSectionCommand _restorePlanSectionCommand;
    private readonly GetSectionCommentsQuery _getSectionCommentsQuery;
    private readonly CreateSectionCommentCommand _createSectionCommentCommand;
    private readonly ResolveSectionCommentCommand _resolveSectionCommentCommand;
    private readonly ReopenSectionCommentCommand _reopenSectionCommentCommand;
    private readonly ArchiveSectionCommentCommand _archiveSectionCommentCommand;
    private readonly ICurrentUserContext _currentUser;

    public PlanSectionsController(
        SavePlanSectionCommand savePlanSectionCommand,
        GetPlanSectionsQuery getPlanSectionsQuery,
        GetPlanSectionByKeyQuery getPlanSectionByKeyQuery,
        GetPlanSectionHistoryQuery getPlanSectionHistoryQuery,
        RestorePlanSectionCommand restorePlanSectionCommand,
        GetSectionCommentsQuery getSectionCommentsQuery,
        CreateSectionCommentCommand createSectionCommentCommand,
        ResolveSectionCommentCommand resolveSectionCommentCommand,
        ReopenSectionCommentCommand reopenSectionCommentCommand,
        ArchiveSectionCommentCommand archiveSectionCommentCommand,
        ICurrentUserContext currentUser)
    {
        _savePlanSectionCommand = savePlanSectionCommand;
        _getPlanSectionsQuery = getPlanSectionsQuery;
        _getPlanSectionByKeyQuery = getPlanSectionByKeyQuery;
        _getPlanSectionHistoryQuery = getPlanSectionHistoryQuery;
        _restorePlanSectionCommand = restorePlanSectionCommand;
        _getSectionCommentsQuery = getSectionCommentsQuery;
        _createSectionCommentCommand = createSectionCommentCommand;
        _resolveSectionCommentCommand = resolveSectionCommentCommand;
        _reopenSectionCommentCommand = reopenSectionCommentCommand;
        _archiveSectionCommentCommand = archiveSectionCommentCommand;
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetSections(Guid planId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

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
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetByKey(Guid planId, string sectionKey, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

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

    [HttpGet("{sectionKey}/history")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetHistory(Guid planId, string sectionKey, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _getPlanSectionHistoryQuery.ExecuteAsync(planId, sectionKey, cancellationToken);
        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return Ok(new { history = result.History });
    }

    [HttpPost("{sectionKey}/restore")]
    [RequireWorkspaceRole("Restore")]
    public async Task<IActionResult> Restore(Guid planId, string sectionKey, [FromBody] RestorePlanSectionBody request, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRestore(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _restorePlanSectionCommand.ExecuteAsync(
            new RestorePlanSectionRequest(planId, sectionKey, request.AuditLogId, request.RestoreReason),
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
                result.SectionId!.Value,
                result.LastEditedByUserId,
                result.LastEditedAtUtc));
    }

    [HttpPut("{sectionKey}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> Save(Guid planId, string sectionKey, [FromBody] SavePlanSectionBody request, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

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

    [HttpGet("{sectionKey}/comments")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetSectionComments(
        Guid planId,
        string sectionKey,
        [FromQuery] bool includeResolved = true,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _getSectionCommentsQuery.ExecuteAsync(planId, sectionKey, includeResolved, cancellationToken);
        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        return Ok(result.Comments);
    }

    [HttpPost("{sectionKey}/comments")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> CreateSectionComment(
        Guid planId,
        string sectionKey,
        [FromBody] CreateSectionCommentBody request,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _createSectionCommentCommand.ExecuteAsync(
            new CreateSectionCommentRequest(planId, sectionKey, request.CommentType, request.CommentText),
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

        return Ok(result.Comment);
    }

    [HttpPost("/api/section-comments/{commentId:guid}/resolve")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ResolveSectionComment(Guid commentId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _resolveSectionCommentCommand.ExecuteAsync(new ResolveSectionCommentRequest(commentId), cancellationToken);
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

        return Ok();
    }

    [HttpPost("/api/section-comments/{commentId:guid}/reopen")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ReopenSectionComment(Guid commentId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _reopenSectionCommentCommand.ExecuteAsync(new ReopenSectionCommentRequest(commentId), cancellationToken);
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

        return Ok();
    }

    [HttpDelete("/api/section-comments/{commentId:guid}")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> ArchiveSectionComment(Guid commentId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _archiveSectionCommentCommand.ExecuteAsync(new ArchiveSectionCommentRequest(commentId), cancellationToken);
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

        return NoContent();
    }
}

public sealed record SavePlanSectionBody(string Title, string Content, int SortOrder);

public sealed record RestorePlanSectionBody(Guid AuditLogId, string? RestoreReason);

public sealed record SavePlanSectionResponse(Guid SectionId, Guid? LastEditedByUserId, DateTimeOffset? LastEditedAtUtc);

public sealed record CreateSectionCommentBody(string CommentType, string CommentText);
