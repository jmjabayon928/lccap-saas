using System.Text;
using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Lccap.Application.Exports.Queries;
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
    private readonly GetActionMatrixExportQuery _actionMatrixExportQuery;
    private readonly GetMonitoringMatrixExportQuery _monitoringMatrixExportQuery;
    private readonly GetFundingReadinessExportQuery _fundingReadinessExportQuery;
    private readonly GetExportPackageManifestQuery _exportPackageManifestQuery;

    public ExportController(
        CreateExportJobCommand createExportJobCommand,
        DownloadExportQuery downloadExportQuery,
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        GetActionMatrixExportQuery actionMatrixExportQuery,
        GetMonitoringMatrixExportQuery monitoringMatrixExportQuery,
        GetFundingReadinessExportQuery fundingReadinessExportQuery,
        GetExportPackageManifestQuery exportPackageManifestQuery)
    {
        _createExportJobCommand = createExportJobCommand;
        _downloadExportQuery = downloadExportQuery;
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _actionMatrixExportQuery = actionMatrixExportQuery;
        _monitoringMatrixExportQuery = monitoringMatrixExportQuery;
        _fundingReadinessExportQuery = fundingReadinessExportQuery;
        _exportPackageManifestQuery = exportPackageManifestQuery;
    }

    [HttpPost("plans/{planId:guid}/exports/pdf")]
    [RequireWorkspaceRole("Export")]
    public async Task<IActionResult> CreatePdfExport(Guid planId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanExport(_currentUserContext.Role))
        {
            return Forbid();
        }

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
    [RequireWorkspaceRole("Export")]
    public Task<IActionResult> GetExportJob(Guid exportJobId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanExport(_currentUserContext.Role))
        {
            return Task.FromResult<IActionResult>(Forbid());
        }

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
    [RequireWorkspaceRole("Export")]
    public async Task<IActionResult> DownloadExport(Guid exportJobId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanExport(_currentUserContext.Role))
        {
            return Forbid();
        }

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

    [HttpGet("plans/{planId:guid}/exports/package-manifest")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetExportPackageManifest(Guid planId, CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUserContext.Role))
        {
            return Forbid();
        }

        var result = await _exportPackageManifestQuery.ExecuteAsync(planId, cancellationToken);
        if (result.Unauthenticated)
        {
            return NotFound();
        }

        if (result.NotFound || result.Manifest is null)
        {
            return NotFound();
        }

        return Ok(result.Manifest);
    }

    [HttpGet("plans/{planId:guid}/exports/action-matrix.csv")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> DownloadActionMatrixCsv(Guid planId, CancellationToken cancellationToken)
    {
        return await PlanCsvResultAsync(planId, $"action-matrix-{planId:D}.csv", _actionMatrixExportQuery.ExecuteAsync, cancellationToken);
    }

    [HttpGet("plans/{planId:guid}/exports/monitoring-matrix.csv")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> DownloadMonitoringMatrixCsv(Guid planId, CancellationToken cancellationToken)
    {
        return await PlanCsvResultAsync(planId, $"monitoring-matrix-{planId:D}.csv", _monitoringMatrixExportQuery.ExecuteAsync, cancellationToken);
    }

    [HttpGet("plans/{planId:guid}/exports/funding-readiness.csv")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> DownloadFundingReadinessCsv(Guid planId, CancellationToken cancellationToken)
    {
        return await PlanCsvResultAsync(planId, $"funding-readiness-{planId:D}.csv", _fundingReadinessExportQuery.ExecuteAsync, cancellationToken);
    }

    private async Task<IActionResult> PlanCsvResultAsync(
        Guid planId,
        string downloadFileName,
        Func<Guid, CancellationToken, Task<PlanExportCsvResult>> execute,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUserContext.Role))
        {
            return Forbid();
        }

        var result = await execute(planId, cancellationToken);
        if (result.Unauthenticated)
        {
            return NotFound();
        }

        if (!result.Success || result.CsvBody is null)
        {
            return NotFound();
        }

        var bytes = Encoding.UTF8.GetBytes(result.CsvBody);
        return File(bytes, "text/csv; charset=utf-8", downloadFileName);
    }
}

public sealed record ExportJobResponse(Guid ExportJobId, string Status, Guid? FileAssetId);
