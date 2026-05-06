using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ExposureAnalysisJobConfiguration : IEntityTypeConfiguration<ExposureAnalysisJob>
{
    public void Configure(EntityTypeBuilder<ExposureAnalysisJob> builder)
    {
        _ = builder.ToTable(
            "exposure_analysis_jobs",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_exposure_analysis_jobs_status",
                    "((status)::text = ANY ((ARRAY['Queued'::character varying, 'Running'::character varying, 'Completed'::character varying, 'Failed'::character varying, 'Cancelled'::character varying])::text[]))");
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

        _ = builder.Property(e => e.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(e => e.PlanId)
            .HasColumnName("plan_id")
            .HasColumnType("uuid")
            .IsRequired();

        _ = builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .IsRequired();

        _ = builder.Property(e => e.InputJson)
            .HasColumnName("input_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.OutputJson)
            .HasColumnName("output_json")
            .HasColumnType("jsonb");

        _ = builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasColumnType("text");

        _ = builder.Property(e => e.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .HasColumnType("timestamptz");

        _ = builder.Property(e => e.CompletedAtUtc)
            .HasColumnName("completed_at_utc")
            .HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .HasColumnType("uuid");

        _ = builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        _ = builder.Property(e => e.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamptz");

        _ = builder.Property(e => e.UpdatedByUserId)
            .HasColumnName("updated_by_user_id")
            .HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValueSql("false")
            .IsRequired();

        _ = builder.Property(e => e.DeletedAtUtc)
            .HasColumnName("deleted_at_utc")
            .HasColumnType("timestamptz");

        _ = builder.Property(e => e.DeletedByUserId)
            .HasColumnName("deleted_by_user_id")
            .HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_exposure_analysis_jobs_plan");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_exposure_analysis_jobs_account");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_analysis_jobs_created_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_analysis_jobs_updated_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_analysis_jobs_deleted_by");
    }
}

