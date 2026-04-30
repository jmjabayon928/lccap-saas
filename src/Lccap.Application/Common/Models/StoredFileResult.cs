namespace Lccap.Application.Common.Models;

public sealed record StoredFileResult(
    string StoredFileName,
    string StoredPath,
    string ContentType,
    string FileExtension,
    long FileSizeBytes,
    string Sha256Hash,
    string StorageProvider);
