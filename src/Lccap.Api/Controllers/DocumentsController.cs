using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentCommand _uploadDocumentCommand;
    private readonly GetDocumentsByPlanQuery _getDocumentsByPlanQuery;

    public DocumentsController(UploadDocumentCommand uploadDocumentCommand, GetDocumentsByPlanQuery getDocumentsByPlanQuery)
    {
        _uploadDocumentCommand = uploadDocumentCommand;
        _getDocumentsByPlanQuery = getDocumentsByPlanQuery;
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
}

public sealed class UploadDocumentFormRequest
{
    public Guid PlanId { get; set; }

    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public IFormFile? File { get; set; }
}
