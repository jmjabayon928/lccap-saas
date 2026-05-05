using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentCommand _uploadDocumentCommand;
    private readonly GetDocumentsByPlanQuery _getDocumentsByPlanQuery;
    private readonly UpdateDocumentMetadataCommand _updateDocumentMetadataCommand;
    private readonly ArchiveDocumentCommand _archiveDocumentCommand;
    private readonly ICurrentUserContext _currentUser;

    public DocumentsController(
        UploadDocumentCommand uploadDocumentCommand,
        GetDocumentsByPlanQuery getDocumentsByPlanQuery,
        UpdateDocumentMetadataCommand updateDocumentMetadataCommand,
        ArchiveDocumentCommand archiveDocumentCommand,
        ICurrentUserContext currentUser)
    {
        _uploadDocumentCommand = uploadDocumentCommand;
        _getDocumentsByPlanQuery = getDocumentsByPlanQuery;
        _updateDocumentMetadataCommand = updateDocumentMetadataCommand;
        _archiveDocumentCommand = archiveDocumentCommand;
        _currentUser = currentUser;
    }

    [HttpPost("api/documents/upload")]
    [Consumes("multipart/form-data")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentFormRequest request, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        ArgumentNullException.ThrowIfNull(request);

        var file = request.File;
        var result = await _uploadDocumentCommand.ExecuteAsync(
            new UploadDocumentRequest(
                request.PlanId,
                request.Category,
                request.Title,
                request.Description,
                file?.OpenReadStream(),
                file?.FileName ?? string.Empty,
                file?.ContentType,
                file?.Length ?? 0),
            cancellationToken);

        if (!result.Success)
        {
            if (result.NotFound)
            {
                return NotFound(new { error = result.Error });
            }

            return BadRequest(new { error = result.Error });
        }

        return Created($"/api/documents/{result.DocumentId}", new { id = result.DocumentId });
    }

    [HttpGet("api/plans/{planId:guid}/documents")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetByPlan(
        [FromRoute] Guid planId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var paged = await _getDocumentsByPlanQuery.ExecuteAsync(planId, page, pageSize, cancellationToken);
        return Ok(new
        {
            items = paged.Items,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalCount = paged.TotalCount
        });
    }

    [HttpPut("api/documents/{documentId:guid}/metadata")]
    [RequireWorkspaceRole("CreateOrEdit")]
    public async Task<IActionResult> UpdateMetadata(
        [FromRoute] Guid documentId,
        [FromBody] UpdateDocumentMetadataApiRequest? body,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanCreateOrEdit(_currentUser.Role))
        {
            return Forbid();
        }

        if (body is null)
        {
            return BadRequest(new { error = "Request body is required." });
        }

        var result = await _updateDocumentMetadataCommand.ExecuteAsync(
            documentId,
            new UpdateDocumentMetadataRequest(
                body.Category,
                body.Title,
                body.Description,
                body.DocumentDate,
                body.SourceAgency,
                body.Tags),
            cancellationToken);

        if (result.Success)
        {
            return Ok(result.Item);
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.UnauthorizedAccount)
        {
            return BadRequest(new { error = "Authenticated account is required." });
        }

        return BadRequest(new { error = result.Error });
    }

    [HttpDelete("api/documents/{documentId:guid}")]
    [RequireWorkspaceRole("Archive")]
    public async Task<IActionResult> Archive([FromRoute] Guid documentId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanArchive(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _archiveDocumentCommand.ExecuteAsync(documentId, cancellationToken);

        if (result.Success)
        {
            return NoContent();
        }

        if (result.NotFound)
        {
            return NotFound();
        }

        if (result.UnauthorizedAccount)
        {
            return BadRequest(new { error = "Authenticated account and user are required." });
        }

        return BadRequest(new { error = "Archive failed." });
    }
}

public sealed class UploadDocumentFormRequest
{
    public Guid PlanId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public IFormFile? File { get; set; }
}

public sealed class UpdateDocumentMetadataApiRequest
{
    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DocumentDate { get; set; }

    public string? SourceAgency { get; set; }

    public string[]? Tags { get; set; }
}
