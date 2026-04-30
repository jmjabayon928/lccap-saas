using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class FileAsset : BaseEntity
{
    public Guid AccountId { get; set; }

    public Account Account { get; set; } = null!;

    public string OwnerType { get; set; } = string.Empty;

    public Guid? OwnerId { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;

    public string StoredFileName { get; set; } = string.Empty;

    public string StoredPath { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public string FileExtension { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string? Sha256Hash { get; set; }

    public string StorageProvider { get; set; } = "Local";

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public Guid? UploadedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public Guid? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAtUtc { get; set; }

    public Guid? DeletedByUserId { get; set; }

    public ICollection<Document> Documents { get; } = new List<Document>();

    public User? UploadedByUser { get; set; }

    public User? CreatedByUser { get; set; }

    public User? UpdatedByUser { get; set; }

    public User? DeletedByUser { get; set; }
}
