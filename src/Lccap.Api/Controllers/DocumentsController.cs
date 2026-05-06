using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
public sealed class DocumentsController : ControllerBase
{
    private readonly UploadDocumentCommand _uploadDocumentCommand;
    private readonly GetDocumentsByPlanQuery _getDocumentsByPlanQuery;
    private readonly GetEvidenceIndexByPlanQuery _getEvidenceIndexByPlanQuery;
    private readonly UpdateDocumentMetadataCommand _updateDocumentMetadataCommand;
    private readonly ArchiveDocumentCommand _archiveDocumentCommand;
    private readonly ICurrentUserContext _currentUser;

    public DocumentsController(
        UploadDocumentCommand uploadDocumentCommand,
        GetDocumentsByPlanQuery getDocumentsByPlanQuery,
        GetEvidenceIndexByPlanQuery getEvidenceIndexByPlanQuery,
        UpdateDocumentMetadataCommand updateDocumentMetadataCommand,
        ArchiveDocumentCommand archiveDocumentCommand,
        ICurrentUserContext currentUser)
    {
        _uploadDocumentCommand = uploadDocumentCommand;
        _getDocumentsByPlanQuery = getDocumentsByPlanQuery;
        _getEvidenceIndexByPlanQuery = getEvidenceIndexByPlanQuery;
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
                request.PlanSectionId,
                request.ActionItemId,
                request.EvidenceStatus,
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

    [HttpGet("api/plans/{planId:guid}/documents/evidence-index")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetEvidenceIndexByPlan([FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _getEvidenceIndexByPlanQuery.ExecuteAsync(planId, cancellationToken);

        if (result.UnauthenticatedAccount)
        {
            return BadRequest(new { error = "Authenticated account is required." });
        }

        if (result.NotFound || result.Result is null)
        {
            return NotFound();
        }

        return Ok(result.Result);
    }

    [HttpGet("api/plans/{planId:guid}/documents/evidence-index.csv")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> DownloadEvidenceIndexCsvByPlan([FromRoute] Guid planId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await _getEvidenceIndexByPlanQuery.ExecuteAsync(planId, cancellationToken);

        if (result.UnauthenticatedAccount)
        {
            return BadRequest(new { error = "Authenticated account is required." });
        }

        if (result.NotFound || result.Result is null)
        {
            return NotFound();
        }

        var csv = BuildEvidenceIndexCsv(result.Result.Items);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var fileName = $"evidence-index-{planId:D}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string BuildEvidenceIndexCsv(IReadOnlyList<EvidenceIndexItem> items)
    {
        static string SafeCell(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var normalized = value;
            var first = normalized.Length > 0 ? normalized[0] : '\0';
            if (first is '=' or '+' or '-' or '@' or '\t' or '\r')
            {
                normalized = $"'{normalized}";
            }

            var needsQuotes = normalized.IndexOfAny([',', '"', '\r', '\n']) >= 0;
            if (!needsQuotes)
            {
                return normalized;
            }

            var escaped = normalized.Replace("\"", "\"\"", StringComparison.Ordinal);
            return $"\"{escaped}\"";
        }

        static string SafeDate(DateOnly? d) =>
            d.HasValue ? d.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) : string.Empty;

        static string SafeInstant(DateTimeOffset d) =>
            d.ToString("O", System.Globalization.CultureInfo.InvariantCulture);

        static string SafeLong(long v) =>
            v.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var sb = new StringBuilder(capacity: Math.Max(256, items.Count * 256));
        sb.AppendLine("DocumentId,Title,Category,EvidenceStatus,SourceAgency,DocumentDate,LinkedSectionKey,LinkedSectionTitle,LinkedActionTitle,LinkedActionType,LinkedActionSector,OriginalFileName,ContentType,FileSizeBytes,Sha256Hash,Tags,CreatedAtUtc");

        foreach (var i in items)
        {
            sb
                .Append(SafeCell(i.DocumentId.ToString("D"))).Append(',')
                .Append(SafeCell(i.Title)).Append(',')
                .Append(SafeCell(i.Category)).Append(',')
                .Append(SafeCell(i.EvidenceStatus)).Append(',')
                .Append(SafeCell(i.SourceAgency)).Append(',')
                .Append(SafeCell(SafeDate(i.DocumentDate))).Append(',')
                .Append(SafeCell(i.PlanSectionKey)).Append(',')
                .Append(SafeCell(i.PlanSectionTitle)).Append(',')
                .Append(SafeCell(i.ActionTitle)).Append(',')
                .Append(SafeCell(i.ActionType)).Append(',')
                .Append(SafeCell(i.ActionSector)).Append(',')
                .Append(SafeCell(i.OriginalFileName)).Append(',')
                .Append(SafeCell(i.ContentType)).Append(',')
                .Append(SafeCell(SafeLong(i.FileSizeBytes))).Append(',')
                .Append(SafeCell(i.Sha256Hash)).Append(',')
                .Append(SafeCell(i.Tags.Count == 0 ? string.Empty : string.Join("; ", i.Tags))).Append(',')
                .Append(SafeCell(SafeInstant(i.CreatedAtUtc)))
                .AppendLine();
        }

        return sb.ToString();
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
                body.PlanSectionId,
                body.ActionItemId,
                body.EvidenceStatus,
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

    public Guid? PlanSectionId { get; set; }

    public Guid? ActionItemId { get; set; }

    public string? EvidenceStatus { get; set; }

    public IFormFile? File { get; set; }
}

public sealed class UpdateDocumentMetadataApiRequest
{
    public string Category { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateOnly? DocumentDate { get; set; }

    public string? SourceAgency { get; set; }

    public Guid? PlanSectionId { get; set; }

    public Guid? ActionItemId { get; set; }

    public string? EvidenceStatus { get; set; }

    public string[]? Tags { get; set; }
}
