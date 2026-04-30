using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class TenantSettingConfiguration : IEntityTypeConfiguration<TenantSetting>
{
    public void Configure(EntityTypeBuilder<TenantSetting> builder)
    {
        _ = builder.ToTable(
            "tenant_settings",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_tenant_settings_key_not_blank",
                    "length(trim(both from setting_key)) > 0");
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

        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid").IsRequired();

        _ = builder.Property(e => e.SettingKey).HasColumnName("setting_key").HasMaxLength(120).IsRequired();

        _ = builder.Property(e => e.SettingValueJson).HasColumnName("setting_value_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").IsRequired().HasDefaultValueSql("false");

        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account)
            .WithMany(a => a.TenantSettings)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_tenant_settings_account");

        _ = builder.HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_settings_created_by");

        _ = builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_settings_updated_by");

        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_tenant_settings_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.SettingKey }).IsUnique()
            .HasDatabaseName("ux_tenant_settings_account_key")
            .HasFilter("is_deleted = false");

        // NOTE: ux_tenant_settings_account_key is not expression-based in baseline SQL, so this mapping is exact.
    }
}
