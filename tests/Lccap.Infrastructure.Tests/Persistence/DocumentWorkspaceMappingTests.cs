using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class DocumentWorkspaceMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55434;Database=lccap_plan_workspace_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Audit_log_entity_maps_to_audit_logs_table()
    {
        using var ctx = CreateContext();

        Assert.Equal("audit_logs", ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName());
    }

    [Fact]
    public void Document_soft_delete_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Document));

        Assert.NotNull(entity);
        Assert.Equal("is_deleted", entity!.FindProperty(nameof(Document.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(Document.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(Document.DeletedByUserId))!.GetColumnName());
    }

    [Fact]
    public void Document_metadata_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Document));

        Assert.NotNull(entity);
        Assert.Equal("document_date", entity!.FindProperty(nameof(Document.DocumentDate))!.GetColumnName());
        Assert.Equal("source_agency", entity.FindProperty(nameof(Document.SourceAgency))!.GetColumnName());
        Assert.Equal("tags_json", entity.FindProperty(nameof(Document.TagsJson))!.GetColumnName());
    }

    [Fact]
    public void Audit_log_row_version_is_bytea_concurrency_token()
    {
        using var ctx = CreateContext();

        var rv = ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(BaseEntity.RowVersion));
        Assert.NotNull(rv);
        Assert.True(rv!.IsConcurrencyToken);
        Assert.Equal("bytea", rv.GetRelationalTypeMapping()?.StoreType);
    }
}
