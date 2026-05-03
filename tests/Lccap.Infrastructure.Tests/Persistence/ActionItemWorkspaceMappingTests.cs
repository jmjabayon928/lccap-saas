using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class ActionItemWorkspaceMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55434;Database=lccap_plan_workspace_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Action_item_soft_delete_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Equal("is_deleted", entity!.FindProperty(nameof(ActionItem.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(ActionItem.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(ActionItem.DeletedByUserId))!.GetColumnName());
    }

    [Fact]
    public void Action_item_editable_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Equal("responsible_office", entity!.FindProperty(nameof(ActionItem.ResponsibleOffice))!.GetColumnName());
        Assert.Equal("budget_amount", entity.FindProperty(nameof(ActionItem.BudgetAmount))!.GetColumnName());
        Assert.Equal("funding_source", entity.FindProperty(nameof(ActionItem.FundingSource))!.GetColumnName());
        Assert.Equal("timeline_start_utc", entity.FindProperty(nameof(ActionItem.TimelineStartUtc))!.GetColumnName());
        Assert.Equal("timeline_end_utc", entity.FindProperty(nameof(ActionItem.TimelineEndUtc))!.GetColumnName());
        Assert.Equal("kpi", entity.FindProperty(nameof(ActionItem.Kpi))!.GetColumnName());
        Assert.Equal("priority_score", entity.FindProperty(nameof(ActionItem.PriorityScore))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(ActionItem.Status))!.GetColumnName());
        Assert.Equal("metadata_json", entity.FindProperty(nameof(ActionItem.MetadataJson))!.GetColumnName());
    }

    [Fact]
    public void Audit_log_entity_maps_for_action_audit_slice()
    {
        using var ctx = CreateContext();
        Assert.Equal("audit_logs", ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName());
    }

    [Fact]
    public void Audit_log_row_version_is_bytea_concurrency_token()
    {
        using var ctx = CreateContext();
        var rv = ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(BaseEntity.RowVersion));
        Assert.NotNull(rv);
        Assert.True(rv!.IsConcurrencyToken);
    }
}
