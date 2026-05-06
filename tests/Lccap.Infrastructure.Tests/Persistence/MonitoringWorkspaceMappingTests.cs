using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class MonitoringWorkspaceMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55434;Database=lccap_plan_workspace_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Monitoring_indicator_soft_delete_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(MonitoringIndicator));

        Assert.NotNull(entity);
        Assert.Equal("is_deleted", entity!.FindProperty(nameof(MonitoringIndicator.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(MonitoringIndicator.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(MonitoringIndicator.DeletedByUserId))!.GetColumnName());
    }

    [Fact]
    public void Monitoring_indicator_editable_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(MonitoringIndicator));

        Assert.NotNull(entity);
        Assert.Equal("action_item_id", entity!.FindProperty(nameof(MonitoringIndicator.ActionItemId))!.GetColumnName());
        Assert.Equal("name", entity.FindProperty(nameof(MonitoringIndicator.Name))!.GetColumnName());
        Assert.Equal("description", entity.FindProperty(nameof(MonitoringIndicator.Description))!.GetColumnName());
        Assert.Equal("baseline_value", entity.FindProperty(nameof(MonitoringIndicator.BaselineValue))!.GetColumnName());
        Assert.Equal("target_value", entity.FindProperty(nameof(MonitoringIndicator.TargetValue))!.GetColumnName());
        Assert.Equal("unit", entity.FindProperty(nameof(MonitoringIndicator.Unit))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(MonitoringIndicator.Status))!.GetColumnName());
        Assert.Equal("metadata_json", entity.FindProperty(nameof(MonitoringIndicator.MetadataJson))!.GetColumnName());
    }

    [Fact]
    public void Monitoring_update_has_indicator_relationship()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(MonitoringUpdate));
        Assert.NotNull(entity);
        var fk = entity!.GetForeignKeys().FirstOrDefault(f => f.PrincipalEntityType.ClrType == typeof(MonitoringIndicator));
        Assert.NotNull(fk);
    }

    [Fact]
    public void Monitoring_update_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(MonitoringUpdate));

        Assert.NotNull(entity);
        Assert.Equal("monitoring_updates", entity!.GetTableName());
        Assert.Equal("account_id", entity.FindProperty(nameof(MonitoringUpdate.AccountId))!.GetColumnName());
        Assert.Equal("monitoring_indicator_id", entity.FindProperty(nameof(MonitoringUpdate.MonitoringIndicatorId))!.GetColumnName());
        Assert.Equal("period_label", entity.FindProperty(nameof(MonitoringUpdate.PeriodLabel))!.GetColumnName());
        Assert.Equal("actual_value", entity.FindProperty(nameof(MonitoringUpdate.ActualValue))!.GetColumnName());
        Assert.Equal("progress_percent", entity.FindProperty(nameof(MonitoringUpdate.ProgressPercent))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(MonitoringUpdate.Status))!.GetColumnName());
        Assert.Equal("notes", entity.FindProperty(nameof(MonitoringUpdate.Notes))!.GetColumnName());
        Assert.Equal("reported_at_utc", entity.FindProperty(nameof(MonitoringUpdate.ReportedAtUtc))!.GetColumnName());
        Assert.Equal("reported_by_user_id", entity.FindProperty(nameof(MonitoringUpdate.ReportedByUserId))!.GetColumnName());
        Assert.Equal("is_deleted", entity.FindProperty(nameof(MonitoringUpdate.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(MonitoringUpdate.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(MonitoringUpdate.DeletedByUserId))!.GetColumnName());

        var rv = entity.FindProperty(nameof(BaseEntity.RowVersion));
        Assert.NotNull(rv);
        Assert.True(rv!.IsConcurrencyToken);
    }

    [Fact]
    public void Audit_log_entity_maps_for_monitoring_audit_slice()
    {
        using var ctx = CreateContext();
        Assert.Equal("audit_logs", ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName());
        var rv = ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(BaseEntity.RowVersion));
        Assert.NotNull(rv);
        Assert.True(rv!.IsConcurrencyToken);
    }
}
