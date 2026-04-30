using System.Linq;
using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class CoreEntityMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55432;Database=lccap_mapping_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Tables_map_to_expected_names_and_schema()
    {
        using var ctx = CreateContext();

        Assert.Equal(
            ("accounts", "public"),
            (ctx.Model.FindEntityType(typeof(Account))!.GetTableName(), ctx.Model.FindEntityType(typeof(Account))!.GetSchema()));

        Assert.Equal(
            ("users", "public"),
            (ctx.Model.FindEntityType(typeof(User))!.GetTableName(), ctx.Model.FindEntityType(typeof(User))!.GetSchema()));

        Assert.Equal(
            ("tenant_settings", "public"),
            (ctx.Model.FindEntityType(typeof(TenantSetting))!.GetTableName(), ctx.Model.FindEntityType(typeof(TenantSetting))!.GetSchema()));

        Assert.Equal(
            ("audit_logs", "public"),
            (ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName(), ctx.Model.FindEntityType(typeof(AuditLog))!.GetSchema()));
    }

    [Fact]
    public void RowVersion_is_mapped_as_bytea_and_concurrency_token()
    {
        using var ctx = CreateContext();

        foreach (var type in new[] { typeof(Account), typeof(User), typeof(TenantSetting), typeof(AuditLog) })
        {
            var entity = ctx.Model.FindEntityType(type)!;
            var rv = entity.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(rv);
            Assert.Equal("bytea", rv!.GetRelationalTypeMapping()?.StoreType);
            Assert.True(rv.IsConcurrencyToken);
        }
    }

    [Fact]
    public void Json_document_columns_are_jsonb_store_types()
    {
        using var ctx = CreateContext();

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(Account))!.FindProperty(nameof(Account.SettingsJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(TenantSetting))!.FindProperty(nameof(TenantSetting.SettingValueJson))!.GetRelationalTypeMapping()?.StoreType);

        foreach (var name in new[]
                     {
                         nameof(AuditLog.OldValuesJson),
                         nameof(AuditLog.NewValuesJson),
                         nameof(AuditLog.MetadataJson),
                     })
        {
            Assert.Equal(
                "jsonb",
                ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(name)!.GetRelationalTypeMapping()?.StoreType);
        }
    }

    [Fact]
    public void Required_foreign_keys_match_baseline_accounts_and_users()
    {
        using var ctx = CreateContext();

        var userFk = ctx.Model.FindEntityType(typeof(User))!.GetForeignKeys()
            .Single(fk => string.Equals(fk.GetConstraintName(), "fk_users_account", StringComparison.Ordinal));

        Assert.Equal(DeleteBehavior.Restrict, userFk.DeleteBehavior);

        Assert.Same(typeof(Account), userFk.PrincipalEntityType.ClrType);

        var tenantFk = ctx.Model.FindEntityType(typeof(TenantSetting))!.GetForeignKeys()
            .Single(fk => string.Equals(fk.GetConstraintName(), "fk_tenant_settings_account", StringComparison.Ordinal));

        Assert.Equal(DeleteBehavior.Restrict, tenantFk.DeleteBehavior);
        Assert.Same(typeof(Account), tenantFk.PrincipalEntityType.ClrType);

        foreach (var (name, expect, principal) in new[]
                     {
                         ("fk_audit_logs_account", DeleteBehavior.SetNull, typeof(Account)),
                         ("fk_audit_logs_user", DeleteBehavior.SetNull, typeof(User)),
                     })
        {
            var fk = ctx.Model.FindEntityType(typeof(AuditLog))!.GetForeignKeys()
                .Single(f => string.Equals(f.GetConstraintName(), name, StringComparison.Ordinal));
            Assert.Equal(expect, fk.DeleteBehavior);
            Assert.Equal(principal, fk.PrincipalEntityType.ClrType);
        }
    }

    [Fact]
    public void Unique_and_filtered_indexes_match_names_from_baseline()
    {
        using var ctx = CreateContext();

        Assert.Contains(
            ctx.Model.FindEntityType(typeof(Account))!.GetIndexes(),
            i => string.Equals(i.GetDatabaseName(), "ix_accounts_status", StringComparison.Ordinal)
                 && string.Equals(i.GetFilter()?.Trim(), "is_deleted = false", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            ctx.Model.FindEntityType(typeof(TenantSetting))!.GetIndexes(),
            i => string.Equals(i.GetDatabaseName(), "ux_tenant_settings_account_key", StringComparison.Ordinal) && i.IsUnique);

        foreach (var name in new[] { "ix_audit_logs_account_created", "ix_audit_logs_user_created", "ix_audit_logs_entity" })
        {
            Assert.Contains(
                ctx.Model.FindEntityType(typeof(AuditLog))!.GetIndexes(),
                i => string.Equals(i.GetDatabaseName(), name, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Lower_expression_indexes_are_not_faked_in_ef_model()
    {
        using var ctx = CreateContext();

        var accountIndexes = ctx.Model.FindEntityType(typeof(Account))!.GetIndexes();
        Assert.DoesNotContain(
            accountIndexes,
            i => string.Equals(i.GetDatabaseName(), "ux_accounts_contact_email", StringComparison.Ordinal));

        var userIndexes = ctx.Model.FindEntityType(typeof(User))!.GetIndexes();
        Assert.DoesNotContain(
            userIndexes,
            i => string.Equals(i.GetDatabaseName(), "ux_users_account_email", StringComparison.Ordinal));
    }

    [Fact]
    public void Entity_index_names_are_unique_per_entity()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[] { typeof(Account), typeof(User), typeof(TenantSetting), typeof(AuditLog) })
        {
            var names = ctx.Model.FindEntityType(clrType)!.GetIndexes()
                .Select(i => i.GetDatabaseName())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList()!;

            Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void Monitoring_entities_are_in_real_model_and_have_required_foreign_key_columns()
    {
        using var ctx = CreateContext();

        var indicator = ctx.Model.FindEntityType(typeof(MonitoringIndicator));
        Assert.NotNull(indicator);
        Assert.Equal("account_id", indicator!.FindProperty(nameof(MonitoringIndicator.AccountId))?.GetColumnName());
        Assert.Equal("plan_id", indicator.FindProperty(nameof(MonitoringIndicator.PlanId))?.GetColumnName());

        var update = ctx.Model.FindEntityType(typeof(MonitoringUpdate));
        Assert.NotNull(update);
        Assert.Equal("account_id", update!.FindProperty(nameof(MonitoringUpdate.AccountId))?.GetColumnName());
        Assert.Equal(
            "monitoring_indicator_id",
            update.FindProperty(nameof(MonitoringUpdate.MonitoringIndicatorId))?.GetColumnName());
    }

    [Fact]
    public void Monitoring_entities_row_version_are_concurrency_tokens()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[] { typeof(MonitoringIndicator), typeof(MonitoringUpdate) })
        {
            var rowVersion = ctx.Model.FindEntityType(clrType)!.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(rowVersion);
            Assert.Equal("bytea", rowVersion!.GetRelationalTypeMapping()?.StoreType);
            Assert.True(rowVersion.IsConcurrencyToken);
        }
    }
}
