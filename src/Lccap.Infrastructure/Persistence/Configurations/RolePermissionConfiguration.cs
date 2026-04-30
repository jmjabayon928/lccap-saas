using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        _ = builder.ToTable(
            "role_permissions",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_role_permissions_effect",
                    "(effect)::text IN ('Allow', 'Deny')");
            });

        builder.ConfigureBaseEntity();

        _ = builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("gen_random_uuid()");
        _ = builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_bytes(8)");

        _ = builder.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.PermissionId).HasColumnName("permission_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.Effect).HasColumnName("effect").HasMaxLength(20).IsRequired().HasDefaultValue("Allow");
        _ = builder.Property(e => e.ConditionJson).HasColumnName("condition_json").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permissions_role");
        _ = builder.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_role_permissions_permission");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_role_permissions_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_role_permissions_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_role_permissions_deleted_by");

        _ = builder.HasIndex(e => e.ConditionJson).HasDatabaseName("ix_role_permissions_condition_json").HasMethod("gin");
        _ = builder.HasIndex(e => e.PermissionId).HasDatabaseName("ix_role_permissions_permission").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.RoleId, e.PermissionId })
            .HasDatabaseName("ux_role_permissions_role_permission")
            .IsUnique()
            .HasFilter("is_deleted = false");
    }
}
