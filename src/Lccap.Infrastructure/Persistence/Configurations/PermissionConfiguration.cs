using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        _ = builder.ToTable(
            "permissions",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_permissions_action_not_blank", "length(trim(both from action_key)) > 0");
                _ = t.HasCheckConstraint("ck_permissions_key_not_blank", "length(trim(both from permission_key)) > 0");
                _ = t.HasCheckConstraint("ck_permissions_module_not_blank", "length(trim(both from module_key)) > 0");
                _ = t.HasCheckConstraint("ck_permissions_name_not_blank", "length(trim(both from permission_name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_permissions_scope_type",
                    "(scope_type)::text IN ('Platform', 'Tenant', 'Resource')");
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

        _ = builder.Property(e => e.PermissionKey).HasColumnName("permission_key").HasMaxLength(150).IsRequired();
        _ = builder.Property(e => e.PermissionName).HasColumnName("permission_name").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.ModuleKey).HasColumnName("module_key").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.ActionKey).HasColumnName("action_key").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.ScopeType).HasColumnName("scope_type").HasMaxLength(30).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.IsSystemPermission).HasColumnName("is_system_permission").HasDefaultValueSql("true").IsRequired();
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_permissions_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_permissions_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_permissions_deleted_by");

        _ = builder.HasIndex(e => e.MetadataJson).HasDatabaseName("ix_permissions_metadata_json").HasMethod("gin");
        _ = builder.HasIndex(e => new { e.ModuleKey, e.ActionKey }).HasDatabaseName("ix_permissions_module_action")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => e.PermissionKey).HasDatabaseName("ux_permissions_key").IsUnique().HasFilter("is_deleted = false");
    }
}
