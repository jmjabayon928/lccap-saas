using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class PlanSectionConfiguration : IEntityTypeConfiguration<PlanSection>
{
    public void Configure(EntityTypeBuilder<PlanSection> builder)
    {
        _ = builder.ToTable(
            "plan_sections",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_plan_sections_key_not_blank", "length(trim(both from section_key)) > 0");
                _ = t.HasCheckConstraint("ck_plan_sections_sort_order", "sort_order >= 0");
                _ = t.HasCheckConstraint("ck_plan_sections_title_not_blank", "length(trim(both from title)) > 0");
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
        _ = builder.Property(e => e.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.SectionKey).HasColumnName("section_key").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.Title).HasColumnName("title").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.Content).HasColumnName("content").HasColumnType("text").IsRequired().HasDefaultValue(string.Empty);
        _ = builder.Property(e => e.SortOrder).HasColumnName("sort_order").HasColumnType("integer").IsRequired();
        _ = builder.Property(e => e.LastEditedByUserId).HasColumnName("last_edited_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.LastEditedAtUtc).HasColumnName("last_edited_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.SectionMetadataJson).HasColumnName("section_metadata_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan)
            .WithMany(p => p.Sections)
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_plan_sections_plan");
        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_plan_sections_account");
        _ = builder.HasOne(e => e.LastEditedByUser).WithMany().HasForeignKey(e => e.LastEditedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plan_sections_last_edited_by");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plan_sections_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plan_sections_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plan_sections_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.PlanId }).HasDatabaseName("ix_plan_sections_account_plan").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.PlanId, e.SortOrder }).HasDatabaseName("ix_plan_sections_plan_sort").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.PlanId, e.SectionKey }).HasDatabaseName("ux_plan_sections_plan_section_key").IsUnique()
            .HasFilter("is_deleted = false");
    }
}
