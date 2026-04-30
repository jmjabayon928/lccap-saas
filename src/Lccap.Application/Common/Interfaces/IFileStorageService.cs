using Lccap.Application.Common.Models;

namespace Lccap.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken);

    Task DeleteAsync(string storedPath, CancellationToken cancellationToken);
}
