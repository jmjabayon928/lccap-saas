using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ActionItemConfiguration : IEntityTypeConfiguration<ActionItem>
{
    public void Configure(EntityTypeBuilder<ActionItem> builder)
    {
        _ = builder.ToTable(
            "action_items",
            "public",
            table =>
            {
                _ = table.HasCheckConstraint(
                    "ck_action_items_title_not_blank",
                    "length(trim(both from title)) > 0");
                _ = table.HasCheckConstraint(
                    "ck_action_items_budget_nonneg",
                    "budget_amount >= 0");
                _ = table.HasCheckConstraint(
                    "ck_action_items_timeline_order",
                    "timeline_start_utc IS NULL OR timeline_end_utc IS NULL OR timeline_start_utc <= timeline_end_utc");
                _ = table.HasCheckConstraint(
                    "ck_action_items_action_type",
                    "(action_type)::text IN ('Adaptation', 'Mitigation')");
                _ = table.HasCheckConstraint(
                    "ck_action_items_status",
                    "(status)::text IN ('Planned', 'InProgress', 'OnTrack', 'Delayed', 'Completed', 'Cancelled')");
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
        _ = builder.Property(e => e.Title).HasColumnName("title").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        _ = builder.Property(e => e.ActionType).HasColumnName("action_type").HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
        _ = builder.Property(e => e.Sector).HasColumnName("sector").HasColumnType("varchar(100)").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.ResponsibleOffice).HasColumnName("responsible_office").HasColumnType("varchar(150)").HasMaxLength(150);
        _ = builder.Property(e => e.BudgetAmount)
            .HasColumnName("budget_amount")
            .HasColumnType("numeric(18,2)")
            .HasDefaultValue(0m)
            .IsRequired();
        _ = builder.Property(e => e.FundingSource).HasColumnName("funding_source").HasColumnType("varchar(150)").HasMaxLength(150);
        _ = builder.Property(e => e.TimelineStartUtc).HasColumnName("timeline_start_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.TimelineEndUtc).HasColumnName("timeline_end_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.Kpi).HasColumnName("kpi").HasColumnType("text");
        _ = builder.Property(e => e.PriorityScore).HasColumnName("priority_score").HasColumnType("numeric(10,4)");
        _ = builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .HasDefaultValue("Planned")
            .IsRequired();
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

        _ = builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_action_items_plan");
        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_action_items_account");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_action_items_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_action_items_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_action_items_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.PlanId }).HasDatabaseName("ix_action_items_account_plan").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.PlanId, e.CreatedAtUtc }).HasDatabaseName("ix_action_items_plan_created").HasFilter("is_deleted = false");
    }
}
