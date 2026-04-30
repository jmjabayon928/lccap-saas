using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    public void Configure(EntityTypeBuilder<ExportJob> builder)
    {
        builder.ToTable(
            "export_jobs",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_export_jobs_export_type",
                    "export_type IN ('Pdf', 'ExcelActions', 'ExcelMonitoring', 'Docx')");
                tableBuilder.HasCheckConstraint(
                    "ck_export_jobs_status",
                    "status IN ('Queued', 'Running', 'Completed', 'Failed', 'Cancelled')");
            });

        builder.ConfigureBaseEntity();

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AccountId).HasColumnName("account_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.PlanId).HasColumnName("plan_id").HasColumnType("uuid").IsRequired();
        builder.Property(x => x.ExportType).HasColumnName("export_type").HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .HasDefaultValue("Queued")
            .IsRequired();
        builder.Property(x => x.FileAssetId).HasColumnName("file_asset_id").HasColumnType("uuid");
        builder.Property(x => x.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
        builder.Property(x => x.OptionsJson)
            .HasColumnName("options_json")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
        builder.Property(x => x.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc").HasColumnType("timestamptz");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        builder.Property(x => x.IsDeleted)
            .HasColumnName("is_deleted")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();
        builder.Property(x => x.RowVersion)
            .HasColumnName("row_version")
            .HasColumnType("bytea")
            .HasDefaultValueSql("gen_random_bytes(8)")
            .IsConcurrencyToken()
            .IsRequired();

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_export_jobs_account");

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(x => x.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_export_jobs_plan");

        builder.HasOne<FileAsset>()
            .WithMany()
            .HasForeignKey(x => x.FileAssetId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_export_jobs_file_asset");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_export_jobs_created_by_user");
    }
}
