using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class MonitoringUpdateConfiguration : IEntityTypeConfiguration<MonitoringUpdate>
{
    public void Configure(EntityTypeBuilder<MonitoringUpdate> builder)
    {
        _ = builder.ToTable(
            "monitoring_updates",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_monitoring_updates_period_label_not_blank", "length(trim(both from period_label)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_monitoring_updates_progress_percent",
                    "progress_percent IS NULL OR (progress_percent >= 0 AND progress_percent <= 100)");
                _ = t.HasCheckConstraint(
                    "ck_monitoring_updates_status",
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
        _ = builder.Property(e => e.MonitoringIndicatorId).HasColumnName("monitoring_indicator_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.PeriodLabel).HasColumnName("period_label").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.ActualValue).HasColumnName("actual_value").HasColumnType("numeric(18,4)");
        _ = builder.Property(e => e.ProgressPercent).HasColumnName("progress_percent").HasColumnType("numeric(5,2)");
        _ = builder.Property(e => e.Status).HasColumnName("status").HasMaxLength(50).IsRequired();
        _ = builder.Property(e => e.Notes).HasColumnName("notes");
        _ = builder.Property(e => e.ReportedAtUtc).HasColumnName("reported_at_utc").HasColumnType("timestamptz").IsRequired();
        _ = builder.Property(e => e.ReportedByUserId).HasColumnName("reported_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");
    }
}
