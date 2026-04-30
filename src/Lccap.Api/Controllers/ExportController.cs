using Lccap.Application.Common.Interfaces;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api")]
public class ExportController : ControllerBase
{
    private readonly CreateExportJobCommand _createExportJobCommand;
    private readonly DownloadExportQuery _downloadExportQuery;
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ExportController(
        CreateExportJobCommand createExportJobCommand,
        DownloadExportQuery downloadExportQuery,
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext)
    {
        _createExportJobCommand = createExportJobCommand;
        _downloadExportQuery = downloadExportQuery;
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    [HttpPost("plans/{planId:guid}/exports/pdf")]
    public async Task<IActionResult> CreatePdfExport(Guid planId, CancellationToken cancellationToken)
    {
        var result = await _createExportJobCommand.ExecuteAsync(
            new CreateExportJobRequest(planId, "Pdf"),
            cancellationToken);

        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }

        if (!result.Success)
        {
            return BadRequest(new { error = result.Error });
        }

        return Created(
            $"/api/exports/{result.ExportJobId}",
            new ExportJobResponse(result.ExportJobId!.Value, result.Status, result.FileAssetId));
    }

    [HttpGet("exports/{exportJobId:guid}")]
    public Task<IActionResult> GetExportJob(Guid exportJobId, CancellationToken cancellationToken)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        var accountId = _currentUserContext.AccountId.Value;
        var job = _dbContext.ExportJobs
            .Where(x => x.Id == exportJobId && x.AccountId == accountId && !x.IsDeleted)
            .Select(
                x => new ExportJobResponse(
                    x.Id,
                    x.Status,
                    x.FileAssetId))
            .SingleOrDefault();

        if (job is null)
        {
            return Task.FromResult<IActionResult>(NotFound());
        }

        return Task.FromResult<IActionResult>(Ok(job));
    }

    [HttpGet("exports/{exportJobId:guid}/download")]
    public async Task<IActionResult> DownloadExport(Guid exportJobId, CancellationToken cancellationToken)
    {
        var result = await _downloadExportQuery.ExecuteAsync(exportJobId, cancellationToken);

        if (result.ForbiddenAccess)
        {
            return Forbid();
        }

        if (!result.Found)
        {
            return NotFound();
        }

        if (result.ConflictState)
        {
            return Conflict();
        }

        return File(result.Stream!, result.ContentType!, result.DownloadFileName);
    }
}

public sealed record ExportJobResponse(Guid ExportJobId, string Status, Guid? FileAssetId);
