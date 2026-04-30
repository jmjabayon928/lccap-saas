using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        _ = builder.ToTable(
            "plans",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_plans_reasonable_years",
                    "((start_year >= 2000 AND start_year <= 2100) AND (end_year >= 2000 AND end_year <= 2100))");
                _ = t.HasCheckConstraint(
                    "ck_plans_status",
                    "(status)::text IN ('Draft', 'InProgress', 'ReadyForExport', 'Submitted', 'Approved', 'Archived')");
                _ = t.HasCheckConstraint(
                    "ck_plans_template_mode",
                    "(template_mode)::text IN ('New', 'Partial', 'Enhancement')");
                _ = t.HasCheckConstraint("ck_plans_title_not_blank", "length(trim(both from title)) > 0");
                _ = t.HasCheckConstraint("ck_plans_year_range", "start_year <= end_year");
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
        _ = builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.StartYear).HasColumnName("start_year").IsRequired();
        _ = builder.Property(e => e.EndYear).HasColumnName("end_year").IsRequired();
        _ = builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired().HasDefaultValue("Draft");
        _ = builder.Property(e => e.TemplateMode).HasColumnName("template_mode").HasMaxLength(50).IsRequired().HasDefaultValue("New");
        _ = builder.Property(e => e.VersionNumber).HasColumnName("version_number").IsRequired().HasDefaultValue(1);
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.SubmittedAtUtc).HasColumnName("submitted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.ApprovedAtUtc).HasColumnName("approved_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account)
            .WithMany(a => a.Plans)
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_plans_account");
        _ = builder.HasOne(e => e.CreatedByUser)
            .WithMany(u => u.CreatedPlans)
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plans_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser)
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plans_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser)
            .WithMany()
            .HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_plans_deleted_by");

        _ = builder.HasMany(e => e.Sections)
            .WithOne(s => s.Plan)
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_plan_sections_plan");
        _ = builder.HasMany(e => e.Documents)
            .WithOne(d => d.Plan)
            .HasForeignKey(d => d.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_documents_plan");

        _ = builder.HasIndex(e => new { e.AccountId, e.Status }).HasDatabaseName("ix_plans_account_status").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.AccountId, e.StartYear, e.EndYear }).HasDatabaseName("ix_plans_account_years").HasFilter("is_deleted = false");

        // Baseline expression index:
        //   ux_plans_account_title_version ON (account_id, lower((title)::text), version_number) WHERE (is_deleted = false)
        // TODO(plan-workspace-migrations): keep this in SQL migrations; EF cannot safely model LOWER(title) expression index.
    }
}
