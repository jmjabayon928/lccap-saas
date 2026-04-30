using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        _ = builder.ToTable(
            "users",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_users_email_not_blank", "length(trim(both from email)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_users_role",
                    "(role)::text IN ('SystemAdmin', 'NationalAdmin', 'AgencyAdmin', 'Admin', 'Planner', 'Reviewer', 'Viewer', 'PublicViewer')");
                _ = t.HasCheckConstraint(
                    "ck_users_scope",
                    "(user_scope)::text IN ('Platform', 'Tenant', 'Public')");
                _ = t.HasCheckConstraint(
                    "ck_users_scope_account_consistency",
                    "(((user_scope)::text = 'Platform'::text AND account_id IS NULL) OR ((user_scope)::text IN ('Tenant', 'Public') AND account_id IS NOT NULL))");
                _ = t.HasCheckConstraint(
                    "ck_users_status",
                    "(status)::text IN ('Active', 'Inactive', 'Suspended', 'Invited')");
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

        _ = builder.Property(e => e.Email).HasColumnName("email").HasMaxLength(255).IsRequired();

        _ = builder.Property(e => e.PasswordHash).HasColumnName("password_hash").IsRequired();

        _ = builder.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(200).IsRequired();

        _ = builder.Property(e => e.Role).HasColumnName("role").HasMaxLength(50).IsRequired();

        _ = builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired().HasDefaultValue("Active");

        _ = builder.Property(e => e.UserScope).HasColumnName("user_scope").HasMaxLength(30).IsRequired().HasDefaultValue("Tenant");

        _ = builder.Property(e => e.LastLoginAtUtc).HasColumnName("last_login_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValueSql("false");

        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account)
            .WithMany(a => a.Users)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_users_account");

        _ = builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_users_created_by");

        _ = builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_users_updated_by");

        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_users_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.Role }).HasDatabaseName("ix_users_account_role")
            .HasFilter("is_deleted = false");

        _ = builder.HasIndex(e => new { e.AccountId, e.Status }).HasDatabaseName("ix_users_account_status")
            .HasFilter("is_deleted = false");

        _ = builder.HasIndex(e => new { e.UserScope, e.Status }).HasDatabaseName("ix_users_scope_status")
            .HasFilter("is_deleted = false");

        // CRITICAL parity note:
        // Baseline defines ux_users_account_email as:
        //   UNIQUE INDEX ... (account_id, lower((email)::text)) WHERE (is_deleted = false)
        // EF Core cannot faithfully model the LOWER(email) expression index here.
        // TODO(rbac-migrations): preserve this as SQL-defined expression index only; avoid fake EF index metadata
        // so migrations do not emit a conflicting plain (account_id, email) unique index.
        //
        // Baseline also defines ux_users_platform_email as:
        //   UNIQUE INDEX ... (lower((email)::text)) WHERE ((account_id IS NULL) AND (is_deleted = false)
        // TODO(rbac-migrations): keep as SQL expression index only.
    }
}
