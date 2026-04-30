using System.Text;
using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Export.Commands;

public class CreateExportJobCommand
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IFileStorageService _fileStorageService;

    public CreateExportJobCommand(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IFileStorageService fileStorageService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _fileStorageService = fileStorageService;
    }

    public virtual async Task<CreateExportJobResult> ExecuteAsync(
        CreateExportJobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue)
        {
            return CreateExportJobResult.ValidationError("Authenticated account is required.");
        }

        var accountId = _currentUserContext.AccountId.Value;
        if (!string.Equals(request.ExportType, "Pdf", StringComparison.OrdinalIgnoreCase))
        {
            return CreateExportJobResult.ValidationError("Only Pdf export type is supported.");
        }

        var plan = await _dbContext.Plans
            .Where(p => p.Id == request.PlanId && p.AccountId == accountId && !p.IsDeleted)
            .Select(p => new { p.Id, p.Title })
            .SingleOrDefaultAsync(cancellationToken);

        if (plan is null)
        {
            return CreateExportJobResult.NotFoundError("Plan not found.");
        }

        var sections = await _dbContext.PlanSections
            .Where(s => s.PlanId == plan.Id && s.AccountId == accountId && !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .Select(s => new ExportSectionLine(s.Title, s.Content))
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var exportJob = new ExportJob
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            PlanId = plan.Id,
            ExportType = "Pdf",
            Status = "Queued",
            OptionsJson = "{}",
            CreatedByUserId = _currentUserContext.UserId,
            CreatedAtUtc = now,
            IsDeleted = false,
        };

        _ = _dbContext.ExportJobs.Add(exportJob);

        try
        {
            exportJob.MarkRunning(DateTimeOffset.UtcNow);

            await using var pdfStream = BuildMinimalPdf(plan.Title, sections);
            var originalFileName = $"{BuildSafeSlug(plan.Title)}-lccap.pdf";
            var storedFile = await _fileStorageService.SaveAsync(
                pdfStream,
                originalFileName,
                "application/pdf",
                accountId,
                cancellationToken);

            var fileAssetId = Guid.NewGuid();
            var fileAsset = new FileAsset
            {
                Id = fileAssetId,
                AccountId = accountId,
                OwnerType = "ExportJob",
                OwnerId = exportJob.Id,
                OriginalFileName = originalFileName,
                StoredFileName = storedFile.StoredFileName,
                StoredPath = storedFile.StoredPath,
                ContentType = storedFile.ContentType,
                FileExtension = storedFile.FileExtension,
                FileSizeBytes = storedFile.FileSizeBytes,
                Sha256Hash = storedFile.Sha256Hash,
                StorageProvider = storedFile.StorageProvider,
                MetadataJson = JsonDocument.Parse("{}"),
                UploadedByUserId = _currentUserContext.UserId,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                CreatedByUserId = _currentUserContext.UserId,
                IsDeleted = false,
            };

            _ = _dbContext.FileAssets.Add(fileAsset);
            exportJob.MarkCompleted(fileAssetId, DateTimeOffset.UtcNow);
            _ = await _dbContext.SaveChangesAsync(cancellationToken);

            return CreateExportJobResult.Created(exportJob.Id, exportJob.Status, exportJob.FileAssetId);
        }
        catch (Exception)
        {
            exportJob.MarkFailed("Export generation failed.");
            try
            {
                _ = await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                // no-op: preserve original failure result
            }

            return CreateExportJobResult.Failure(exportJob.Id, exportJob.Status, exportJob.FileAssetId, "Export generation failed.");
        }
    }

    private static MemoryStream BuildMinimalPdf(string planTitle, IReadOnlyCollection<ExportSectionLine> sections)
    {
        static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

        var lines = new List<string> { $"LCCAP Export: {planTitle}" };
        foreach (var section in sections)
        {
            lines.Add($"{section.Title}: {section.Content}");
        }

        var y = 780;
        var contentBuilder = new StringBuilder();
        contentBuilder.AppendLine("BT");
        contentBuilder.AppendLine("/F1 12 Tf");
        foreach (var line in lines)
        {
            contentBuilder.AppendLine($"1 0 0 1 40 {y} Tm ({EscapePdf(line)}) Tj");
            y -= 18;
            if (y < 60)
            {
                break;
            }
        }

        contentBuilder.AppendLine("ET");
        var streamBody = contentBuilder.ToString();
        var streamBytes = Encoding.ASCII.GetBytes(streamBody);

        var objects = new[]
        {
            "1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n",
            "2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj\n",
            "3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >> endobj\n",
            "4 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\n",
            $"5 0 obj << /Length {streamBytes.Length} >> stream\n{streamBody}endstream\nendobj\n",
        };

        var builder = new StringBuilder();
        builder.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        foreach (var obj in objects)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString()));
            builder.Append(obj);
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(builder.ToString());
        builder.Append($"xref\n0 {offsets.Count}\n");
        builder.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            builder.Append($"{offsets[i]:D10} 00000 n \n");
        }

        builder.Append("trailer << /Size 6 /Root 1 0 R >>\n");
        builder.Append($"startxref\n{xrefOffset}\n%%EOF");

        return new MemoryStream(Encoding.ASCII.GetBytes(builder.ToString()));
    }

    private static string BuildSafeSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "plan-export";
        }

        var sanitized = new string(
            title
                .Trim()
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray());
        var collapsed = string.Join("-", sanitized.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(collapsed) ? "plan-export" : collapsed;
    }
}

internal sealed record ExportSectionLine(string Title, string Content);

public sealed record CreateExportJobRequest(Guid PlanId, string ExportType);

public sealed record CreateExportJobResult(
    bool Success,
    bool NotFound,
    Guid? ExportJobId,
    string Status,
    Guid? FileAssetId,
    string? Error)
{
    public static CreateExportJobResult Created(Guid exportJobId, string status, Guid? fileAssetId) =>
        new(true, false, exportJobId, status, fileAssetId, null);

    public static CreateExportJobResult ValidationError(string error) =>
        new(false, false, null, "Failed", null, error);

    public static CreateExportJobResult NotFoundError(string error) =>
        new(false, true, null, "Failed", null, error);

    public static CreateExportJobResult Failure(Guid exportJobId, string status, Guid? fileAssetId, string error) =>
        new(false, false, exportJobId, status, fileAssetId, error);
}
