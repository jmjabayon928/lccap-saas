using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        _ = builder.ToTable(
            "user_roles",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_user_roles_dates",
                    "starts_at_utc IS NULL OR expires_at_utc IS NULL OR starts_at_utc <= expires_at_utc");
                _ = t.HasCheckConstraint(
                    "ck_user_roles_scope_account",
                    "(((scope_type)::text = 'Platform'::text AND account_id IS NULL AND resource_type IS NULL AND resource_id IS NULL) OR ((scope_type)::text = 'Tenant'::text AND account_id IS NOT NULL AND resource_type IS NULL AND resource_id IS NULL) OR ((scope_type)::text = 'Resource'::text AND account_id IS NOT NULL AND resource_type IS NOT NULL AND resource_id IS NOT NULL))");
                _ = t.HasCheckConstraint(
                    "ck_user_roles_scope_type",
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

        _ = builder.Property(e => e.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.RoleId).HasColumnName("role_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid");
        _ = builder.Property(e => e.ScopeType).HasColumnName("scope_type").HasMaxLength(30).IsRequired();
        _ = builder.Property(e => e.ResourceType).HasColumnName("resource_type").HasMaxLength(80);
        _ = builder.Property(e => e.ResourceId).HasColumnName("resource_id").HasColumnType("uuid");
        _ = builder.Property(e => e.AssignedAtUtc).HasColumnName("assigned_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.StartsAtUtc).HasColumnName("starts_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.AssignedByUserId).HasColumnName("assigned_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.AssignmentReason).HasColumnName("assignment_reason");
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_roles_user");
        _ = builder.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_roles_role");
        _ = builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_user_roles_account");
        _ = builder.HasOne(e => e.AssignedByUser).WithMany().HasForeignKey(e => e.AssignedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_user_roles_assigned_by");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_user_roles_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_user_roles_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_user_roles_deleted_by");

        _ = builder.HasIndex(e => e.AccountId).HasDatabaseName("ix_user_roles_account").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => e.MetadataJson).HasDatabaseName("ix_user_roles_metadata_json").HasMethod("gin");
        _ = builder.HasIndex(e => new { e.AccountId, e.ResourceType, e.ResourceId }).HasDatabaseName("ix_user_roles_resource")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => e.RoleId).HasDatabaseName("ix_user_roles_role").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.UserId, e.ScopeType }).HasDatabaseName("ix_user_roles_user_scope")
            .HasFilter("is_deleted = false");

        // CRITICAL parity note:
        // Baseline ux_user_roles_assignment is expression-based with COALESCE(...) on account_id/resource_type/resource_id.
        // TODO(rbac-migrations): keep expression unique index in SQL migrations only; EF cannot safely model this as a
        // standard property index without generating a conflicting shape.
    }
}
