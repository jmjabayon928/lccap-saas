using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        _ = builder.ToTable(
            "accounts",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_accounts_contact_email_not_blank",
                    "length(trim(both from contact_email)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_accounts_status",
                    "(status)::text IN ('Active', 'Inactive', 'Suspended')");
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

        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        _ = builder.Property(e => e.Region).HasColumnName("region").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.Province).HasColumnName("province").HasMaxLength(100).IsRequired();

        _ = builder.Property(e => e.MunicipalityOrCity).HasColumnName("municipality_or_city").HasMaxLength(150).IsRequired();

        _ = builder.Property(e => e.LguType).HasColumnName("lgu_type").HasMaxLength(50).IsRequired();

        _ = builder.Property(e => e.ContactEmail).HasColumnName("contact_email").HasMaxLength(255).IsRequired();

        _ = builder.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(50);

        _ = builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired().HasDefaultValue("Active");

        _ = builder.Property(e => e.SettingsJson).HasColumnName("settings_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValueSql("false");

        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        AuditNavigations(builder);

        _ = builder.HasIndex(e => new { e.Region, e.Province }).HasDatabaseName("ix_accounts_region_province");

        _ = builder.HasIndex(e => e.Status).HasDatabaseName("ix_accounts_status").HasFilter("is_deleted = false");

        // CRITICAL parity note:
        // Baseline defines ux_accounts_contact_email as:
        //   UNIQUE INDEX ... (lower((contact_email)::text)) WHERE (is_deleted = false)
        // EF Core cannot model expression indexes in the portable fluent API without provider SQL annotations.
        // TODO(rbac-migrations): keep this index in SQL migrations only; do NOT add a fake property index here,
        // otherwise EF will try to create a conflicting non-expression index with the same intent.
    }

    private static void AuditNavigations(EntityTypeBuilder<Account> builder)
    {
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_accounts_created_by");

        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_accounts_updated_by");

        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_accounts_deleted_by");
    }
}
