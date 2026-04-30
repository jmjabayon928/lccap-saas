namespace Lccap.Api.Options;

public sealed class FileStorageOptions
{
    public string RootPath { get; set; } = "uploads";

    public long MaxUploadBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedExtensions { get; set; } = [".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg"];
}
