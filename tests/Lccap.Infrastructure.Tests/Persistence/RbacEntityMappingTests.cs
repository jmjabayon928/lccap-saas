using System.Linq;
using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class RbacEntityMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55433;Database=lccap_rbac_mapping_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Rbac_tables_map_to_expected_names()
    {
        using var ctx = CreateContext();

        Assert.Equal("roles", ctx.Model.FindEntityType(typeof(Role))!.GetTableName());
        Assert.Equal("permissions", ctx.Model.FindEntityType(typeof(Permission))!.GetTableName());
        Assert.Equal("role_permissions", ctx.Model.FindEntityType(typeof(RolePermission))!.GetTableName());
        Assert.Equal("user_roles", ctx.Model.FindEntityType(typeof(UserRole))!.GetTableName());
    }

    [Fact]
    public void Rbac_row_version_is_concurrency_token_bytea()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[] { typeof(Role), typeof(Permission), typeof(RolePermission), typeof(UserRole) })
        {
            var prop = ctx.Model.FindEntityType(clrType)!.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(prop);
            Assert.True(prop!.IsConcurrencyToken);
            Assert.Equal("bytea", prop.GetRelationalTypeMapping()?.StoreType);
        }
    }

    [Fact]
    public void Rbac_jsonb_columns_are_mapped_to_jsonb()
    {
        using var ctx = CreateContext();

        Assert.Equal("jsonb", ctx.Model.FindEntityType(typeof(Role))!.FindProperty(nameof(Role.MetadataJson))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal("jsonb", ctx.Model.FindEntityType(typeof(Permission))!.FindProperty(nameof(Permission.MetadataJson))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal("jsonb", ctx.Model.FindEntityType(typeof(RolePermission))!.FindProperty(nameof(RolePermission.ConditionJson))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal("jsonb", ctx.Model.FindEntityType(typeof(UserRole))!.FindProperty(nameof(UserRole.MetadataJson))!.GetRelationalTypeMapping()?.StoreType);
    }

    [Fact]
    public void Rbac_foreign_keys_exist_with_expected_constraint_names()
    {
        using var ctx = CreateContext();

        var rpType = ctx.Model.FindEntityType(typeof(RolePermission))!;
        Assert.Contains(rpType.GetForeignKeys(), fk => fk.GetConstraintName() == "fk_role_permissions_role");
        Assert.Contains(rpType.GetForeignKeys(), fk => fk.GetConstraintName() == "fk_role_permissions_permission");

        var urType = ctx.Model.FindEntityType(typeof(UserRole))!;
        Assert.Contains(urType.GetForeignKeys(), fk => fk.GetConstraintName() == "fk_user_roles_user");
        Assert.Contains(urType.GetForeignKeys(), fk => fk.GetConstraintName() == "fk_user_roles_role");
        Assert.Contains(urType.GetForeignKeys(), fk => fk.GetConstraintName() == "fk_user_roles_account");
    }

    [Fact]
    public void Rbac_unique_and_filtered_indexes_are_mapped_where_safe()
    {
        using var ctx = CreateContext();

        var permissionIndexes = ctx.Model.FindEntityType(typeof(Permission))!.GetIndexes();
        Assert.Contains(permissionIndexes, ix => ix.GetDatabaseName() == "ux_permissions_key" && ix.IsUnique);
        Assert.Contains(permissionIndexes, ix => ix.GetDatabaseName() == "ix_permissions_module_action" && ix.GetFilter() != null);

        var roleIndexes = ctx.Model.FindEntityType(typeof(Role))!.GetIndexes();
        Assert.Contains(roleIndexes, ix => ix.GetDatabaseName() == "ux_roles_account_key" && ix.IsUnique);
        Assert.Contains(roleIndexes, ix => ix.GetDatabaseName() == "ux_roles_global_key" && ix.IsUnique);

        var rolePermIndexes = ctx.Model.FindEntityType(typeof(RolePermission))!.GetIndexes();
        Assert.Contains(rolePermIndexes, ix => ix.GetDatabaseName() == "ux_role_permissions_role_permission" && ix.IsUnique);

        var userRoleIndexes = ctx.Model.FindEntityType(typeof(UserRole))!.GetIndexes();
        Assert.DoesNotContain(userRoleIndexes, ix => ix.GetDatabaseName() == "ux_user_roles_assignment");
    }

    [Fact]
    public void Rbac_entity_index_names_have_no_duplicates()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[] { typeof(Role), typeof(Permission), typeof(RolePermission), typeof(UserRole) })
        {
            var names = ctx.Model.FindEntityType(clrType)!.GetIndexes()
                .Select(ix => ix.GetDatabaseName())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .ToList()!;

            Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        }
    }
}
