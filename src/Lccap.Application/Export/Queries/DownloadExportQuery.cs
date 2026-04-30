using Lccap.Application.Common.Interfaces;

namespace Lccap.Application.Export.Queries;

public class DownloadExportQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFileStorageService _fileStorageService;

    public DownloadExportQuery(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _fileStorageService = fileStorageService;
    }

    public virtual async Task<DownloadExportResult> ExecuteAsync(Guid exportJobId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return DownloadExportResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var exportJob = _dbContext.ExportJobs
            .Where(x => x.Id == exportJobId && x.AccountId == accountId && !x.IsDeleted)
            .Select(x => new { x.Status, x.FileAssetId })
            .SingleOrDefault();

        if (exportJob is null)
        {
            return DownloadExportResult.NotFound();
        }

        if (!string.Equals(exportJob.Status, "Completed", StringComparison.Ordinal))
        {
            return DownloadExportResult.Conflict();
        }

        if (!exportJob.FileAssetId.HasValue)
        {
            return DownloadExportResult.Conflict();
        }

        var fileAsset = _dbContext.FileAssets
            .Where(x => x.Id == exportJob.FileAssetId.Value && x.AccountId == accountId && !x.IsDeleted)
            .Select(x => new { x.OriginalFileName, x.ContentType, x.StoredPath })
            .SingleOrDefault();

        if (fileAsset is null)
        {
            return DownloadExportResult.NotFound();
        }

        var stream = await _fileStorageService.OpenReadAsync(fileAsset.StoredPath, cancellationToken);
        var downloadName = string.IsNullOrWhiteSpace(fileAsset.OriginalFileName) ? "export.pdf" : fileAsset.OriginalFileName;
        var contentType = string.IsNullOrWhiteSpace(fileAsset.ContentType) ? "application/octet-stream" : fileAsset.ContentType;
        return DownloadExportResult.Success(stream, downloadName, contentType);
    }
}

public sealed record DownloadExportResult(
    bool Found,
    bool ForbiddenAccess,
    bool ConflictState,
    Stream? Stream,
    string? DownloadFileName,
    string? ContentType)
{
    public static DownloadExportResult Success(Stream stream, string downloadFileName, string contentType) =>
        new(true, false, false, stream, downloadFileName, contentType);

    public static DownloadExportResult NotFound() => new(false, false, false, null, null, null);

    public static DownloadExportResult Conflict() => new(true, false, true, null, null, null);

    public static DownloadExportResult Forbidden() => new(false, true, false, null, null, null);
}
