using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        _ = builder.ToTable(
            "roles",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_roles_key_not_blank", "length(trim(both from role_key)) > 0");
                _ = t.HasCheckConstraint("ck_roles_name_not_blank", "length(trim(both from role_name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_roles_platform_account",
                    "(((role_scope)::text = 'Platform'::text AND account_id IS NULL) OR ((role_scope)::text IN ('Tenant', 'Public')))");
                _ = t.HasCheckConstraint("ck_roles_priority", "priority >= 0");
                _ = t.HasCheckConstraint(
                    "ck_roles_scope",
                    "(role_scope)::text IN ('Platform', 'Tenant', 'Public')");
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

        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid");
        _ = builder.Property(e => e.RoleKey).HasColumnName("role_key").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.RoleName).HasColumnName("role_name").HasMaxLength(200).IsRequired();
        _ = builder.Property(e => e.RoleScope).HasColumnName("role_scope").HasMaxLength(30).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.IsSystemRole).HasColumnName("is_system_role").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.IsAssignable).HasColumnName("is_assignable").HasDefaultValueSql("true").IsRequired();
        _ = builder.Property(e => e.Priority).HasColumnName("priority").HasDefaultValue(100).IsRequired();
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_roles_account");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_roles_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_roles_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_roles_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.RoleScope })
            .HasDatabaseName("ix_roles_account_scope")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => e.MetadataJson).HasDatabaseName("ix_roles_metadata_json").HasMethod("gin");

        _ = builder.HasIndex(e => new { e.AccountId, e.RoleKey })
            .HasDatabaseName("ux_roles_account_key")
            .IsUnique()
            .HasFilter("account_id IS NOT NULL AND is_deleted = false");
        _ = builder.HasIndex(e => e.RoleKey)
            .HasDatabaseName("ux_roles_global_key")
            .IsUnique()
            .HasFilter("account_id IS NULL AND is_deleted = false");
    }
}
