using System.Security.Cryptography;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Common.Models;
using Microsoft.Extensions.Configuration;

namespace Lccap.Infrastructure.Storage;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const string StorageProviderName = "Local";
    private const string FileStorageSection = "FileStorage";
    private string _rootPath = string.Empty;
    private long _maxUploadBytes;
    private HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase);

    public LocalFileStorageService(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(FileStorageSection);
        var configuredRoot = section["RootPath"];
        var maxUploadBytes = long.TryParse(section["MaxUploadBytes"], out var parsedMaxUploadBytes)
            ? parsedMaxUploadBytes
            : 10 * 1024 * 1024;
        var configuredExtensions = section
            .GetSection("AllowedExtensions")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        Initialize(
            string.IsNullOrWhiteSpace(configuredRoot)
                ? Path.Combine(AppContext.BaseDirectory, "uploads")
                : configuredRoot,
            maxUploadBytes,
            configuredExtensions);
    }

    public LocalFileStorageService(string rootPath, long maxUploadBytes, IEnumerable<string> allowedExtensions)
    {
        Initialize(rootPath, maxUploadBytes, allowedExtensions);
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.CanSeek && stream.Length <= 0)
        {
            throw new InvalidOperationException("File stream cannot be empty.");
        }

        var extension = NormalizeExtension(Path.GetExtension(originalFileName));
        if (!_allowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File extension is not allowed.");
        }

        var now = DateTime.UtcNow;
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = Path.Combine(
            "uploads",
            accountId.ToString(),
            now.ToString("yyyy"),
            now.ToString("MM"),
            storedFileName);

        var fullPath = GetSafeFullPath(relativePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Failed to resolve storage directory.");
        Directory.CreateDirectory(directory);

        if (File.Exists(fullPath))
        {
            throw new IOException("Stored file path already exists.");
        }

        if (stream.CanSeek && stream.Position != 0)
        {
            _ = stream.Seek(0, SeekOrigin.Begin);
        }

        long totalBytes = 0;
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        var buffer = new byte[81920];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            totalBytes += read;
            if (totalBytes > _maxUploadBytes)
            {
                await output.DisposeAsync();
                File.Delete(fullPath);
                throw new InvalidOperationException("File exceeds maximum upload size.");
            }

            sha.AppendData(buffer, 0, read);
            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (totalBytes <= 0)
        {
            File.Delete(fullPath);
            throw new InvalidOperationException("File stream cannot be empty.");
        }

        var hash = Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
        return new StoredFileResult(
            StoredFileName: storedFileName,
            StoredPath: relativePath.Replace('\\', '/'),
            ContentType: string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            FileExtension: extension,
            FileSizeBytes: totalBytes,
            Sha256Hash: hash,
            StorageProvider: StorageProviderName);
    }

    public Task<Stream> OpenReadAsync(string storedPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = GetSafeFullPath(storedPath);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = GetSafeFullPath(storedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("File extension is required.");
        }

        var normalized = extension.Trim().ToLowerInvariant();
        if (!normalized.StartsWith('.'))
        {
            normalized = $".{normalized}";
        }

        foreach (var character in normalized)
        {
            if (!(character is '.' or >= 'a' and <= 'z' or >= '0' and <= '9'))
            {
                throw new InvalidOperationException("File extension is invalid.");
            }
        }

        return normalized;
    }

    private string GetSafeFullPath(string storedPath)
    {
        if (string.IsNullOrWhiteSpace(storedPath))
        {
            throw new ArgumentException("Stored path is required.", nameof(storedPath));
        }

        var normalizedRelativePath = storedPath
            .Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedRelativePath));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : $"{_rootPath}{Path.DirectorySeparatorChar}";

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Path traversal detected.");
        }

        return fullPath;
    }

    private void Initialize(string rootPath, long maxUploadBytes, IEnumerable<string> allowedExtensions)
    {
        _rootPath = Path.GetFullPath(
            string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(AppContext.BaseDirectory, "uploads")
                : rootPath);
        _maxUploadBytes = maxUploadBytes > 0 ? maxUploadBytes : 10 * 1024 * 1024;

        var extensionSource = allowedExtensions?.ToArray() is { Length: > 0 } configured
            ? configured
            : [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"];
        _allowedExtensions = new HashSet<string>(
            extensionSource.Select(NormalizeExtension),
            StringComparer.OrdinalIgnoreCase);
    }
}
