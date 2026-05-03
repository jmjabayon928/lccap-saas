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

    public DocumentsController(
        UploadDocumentCommand uploadDocumentCommand,
        GetDocumentsByPlanQuery getDocumentsByPlanQuery,
        UpdateDocumentMetadataCommand updateDocumentMetadataCommand,
        ArchiveDocumentCommand archiveDocumentCommand)
    {
        _uploadDocumentCommand = uploadDocumentCommand;
        _getDocumentsByPlanQuery = getDocumentsByPlanQuery;
        _updateDocumentMetadataCommand = updateDocumentMetadataCommand;
        _archiveDocumentCommand = archiveDocumentCommand;
    }

    [HttpPost("api/documents/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadDocumentFormRequest request, CancellationToken cancellationToken)
    {
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
    public async Task<IActionResult> GetByPlan([FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        var documents = await _getDocumentsByPlanQuery.ExecuteAsync(planId, cancellationToken);
        return Ok(documents);
    }

    [HttpPut("api/documents/{documentId:guid}/metadata")]
    public async Task<IActionResult> UpdateMetadata(
        [FromRoute] Guid documentId,
        [FromBody] UpdateDocumentMetadataApiRequest? body,
        CancellationToken cancellationToken)
    {
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
    public async Task<IActionResult> Archive([FromRoute] Guid documentId, CancellationToken cancellationToken)
    {
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
