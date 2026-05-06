using System.Text.Json;
using Lccap.Domain.Common.Entities;

namespace Lccap.Domain.Entities;

public sealed class NotificationTemplate : BaseEntity
{
    public Guid? AccountId { get; set; }

    public string TemplateKey { get; set; } = string.Empty;

    public string? Subject { get; set; }

    public string Body { get; set; } = string.Empty;

    public JsonDocument MetadataJson { get; set; } = JsonDocument.Parse("{}");

    public DateTimeOffset CreatedAtUtc { get; set; }

    public bool IsDeleted { get; set; }

    public Account? Account { get; set; }
}

