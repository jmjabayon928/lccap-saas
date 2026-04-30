using System.Security.Cryptography;
using System.Text;
using Lccap.Infrastructure.Storage;

namespace Lccap.Infrastructure.Tests.Storage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "lccap-storage-tests", Guid.NewGuid().ToString("N"));
        _service = new LocalFileStorageService(_tempRoot, 1048576, [".pdf", ".jpg"]);
    }

    [Fact]
    public async Task SaveAsync_WritesFileUnderTenantScopedFolder()
    {
        await using var stream = CreateStream("tenant path check");
        var accountId = Guid.NewGuid();

        var result = await _service.SaveAsync(stream, "evidence.pdf", "application/pdf", accountId, CancellationToken.None);

        Assert.StartsWith($"uploads/{accountId}/", result.StoredPath, StringComparison.OrdinalIgnoreCase);
        var fullPath = Path.Combine(_tempRoot, result.StoredPath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task SaveAsync_GeneratesStoredFileName_NotOriginalName()
    {
        await using var stream = CreateStream("name check");

        var result = await _service.SaveAsync(stream, "my-original-name.pdf", "application/pdf", Guid.NewGuid(), CancellationToken.None);

        Assert.NotEqual("my-original-name.pdf", result.StoredFileName);
        Assert.EndsWith(".pdf", result.StoredFileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_ProducesSha256()
    {
        const string content = "hash me";
        await using var stream = CreateStream(content);

        var result = await _service.SaveAsync(stream, "doc.pdf", "application/pdf", Guid.NewGuid(), CancellationToken.None);

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
        Assert.Equal(expectedHash, result.Sha256Hash);
    }

    [Fact]
    public async Task OpenReadAsync_ReadsSavedFile()
    {
        const string content = "read me";
        await using var stream = CreateStream(content);
        var result = await _service.SaveAsync(stream, "doc.pdf", "application/pdf", Guid.NewGuid(), CancellationToken.None);

        await using var readStream = await _service.OpenReadAsync(result.StoredPath, CancellationToken.None);
        using var reader = new StreamReader(readStream, Encoding.UTF8);
        var loaded = await reader.ReadToEndAsync();

        Assert.Equal(content, loaded);
    }

    [Fact]
    public async Task DeleteAsync_DeletesSavedFile()
    {
        await using var stream = CreateStream("delete me");
        var result = await _service.SaveAsync(stream, "doc.pdf", "application/pdf", Guid.NewGuid(), CancellationToken.None);
        var fullPath = Path.Combine(_tempRoot, result.StoredPath.Replace('/', Path.DirectorySeparatorChar));

        await _service.DeleteAsync(result.StoredPath, CancellationToken.None);

        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task PathTraversalAttempt_IsRejected()
    {
        var traversal = $"..{Path.DirectorySeparatorChar}..{Path.DirectorySeparatorChar}outside.txt";

        _ = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _service.OpenReadAsync(traversal, CancellationToken.None));
    }

    [Fact]
    public async Task DisallowedExtension_IsRejected()
    {
        await using var stream = CreateStream("not allowed");

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SaveAsync(stream, "payload.exe", "application/octet-stream", Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task EmptyStream_IsRejected()
    {
        await using var stream = new MemoryStream();

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SaveAsync(stream, "empty.pdf", "application/pdf", Guid.NewGuid(), CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private static MemoryStream CreateStream(string content) => new(Encoding.UTF8.GetBytes(content));
}
