using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ClimateExpenditureTagConfiguration : IEntityTypeConfiguration<ClimateExpenditureTag>
{
    public void Configure(EntityTypeBuilder<ClimateExpenditureTag> builder)
    {
        _ = builder.ToTable(
            "climate_expenditure_tags",
            "public",
            table =>
            {
                _ = table.HasCheckConstraint(
                    "ck_climate_expenditure_tags_category",
                    "(tag_category)::text IN ('Adaptation', 'Mitigation', 'CrossCutting', 'DisasterRiskReduction', 'CapacityDevelopment', 'Other')");
                _ = table.HasCheckConstraint(
                    "ck_climate_expenditure_tags_code_not_blank",
                    "length(trim(both from tag_code)) > 0");
                _ = table.HasCheckConstraint(
                    "ck_climate_expenditure_tags_name_not_blank",
                    "length(trim(both from tag_name)) > 0");
                _ = table.HasCheckConstraint(
                    "ck_climate_expenditure_tags_weight",
                    "weight_percent IS NULL OR (weight_percent >= 0 AND weight_percent <= 100)");
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
        _ = builder.Property(e => e.TagCode).HasColumnName("tag_code").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.TagName).HasColumnName("tag_name").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.TagCategory).HasColumnName("tag_category").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.WeightPercent)
            .HasColumnName("weight_percent")
            .HasColumnType("numeric(5,2)")
            .HasPrecision(5, 2);
        _ = builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        _ = builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        _ = builder.Property(e => e.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_climate_expenditure_tags_account");
    }
}
