using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class MonitoringIndicatorConfiguration : IEntityTypeConfiguration<MonitoringIndicator>
{
    public void Configure(EntityTypeBuilder<MonitoringIndicator> builder)
    {
        _ = builder.ToTable(
            "monitoring_indicators",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_monitoring_indicators_name_not_blank", "length(trim(both from name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_monitoring_indicators_status",
                    "(status)::text IN ('NotStarted', 'InProgress', 'OnTrack', 'Delayed', 'Completed')");
            });

        builder.ConfigureBaseEntity();

        _ = builder.Property(e => e.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedOnAdd().HasDefaultValueSql("gen_random_uuid()");
        _ = builder.Property(e => e.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .IsConcurrencyToken()
            .HasDefaultValueSql("gen_random_bytes(8)");

        _ = builder.Property(e => e.AccountId).HasColumnName("account_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.ActionItemId).HasColumnName("action_item_id").HasColumnType("uuid");
        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.BaselineValue).HasColumnName("baseline_value").HasColumnType("numeric(18,4)");
        _ = builder.Property(e => e.TargetValue).HasColumnName("target_value").HasColumnType("numeric(18,4)");
        _ = builder.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(80);
        _ = builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired().HasDefaultValue("NotStarted");
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired().HasDefaultValue("{}");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(e => e.Updates)
            .WithOne(u => u.MonitoringIndicator)
            .HasForeignKey(u => u.MonitoringIndicatorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
