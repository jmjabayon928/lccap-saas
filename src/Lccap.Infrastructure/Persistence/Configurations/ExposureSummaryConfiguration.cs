using System.Text.Json;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ExposureSummaryConfiguration : IEntityTypeConfiguration<ExposureSummary>
{
    public void Configure(EntityTypeBuilder<ExposureSummary> builder)
    {
        _ = builder.ToTable(
            "exposure_summaries",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_exposure_summaries_area",
                    "((exposed_area_hectares IS NULL) OR (exposed_area_hectares >= (0)::numeric))");
                _ = t.HasCheckConstraint(
                    "ck_exposure_summaries_facility_count",
                    "((exposed_facility_count >= 0))");
                _ = t.HasCheckConstraint(
                    "ck_exposure_summaries_population",
                    "((exposed_population IS NULL) OR (exposed_population >= (0)::numeric))");
                _ = t.HasCheckConstraint(
                    "ck_exposure_summaries_risk",
                    "((risk_score IS NULL) OR (risk_score >= (0)::numeric))");
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

        _ = builder.Property(e => e.ExposureAnalysisJobId)
            .HasColumnName("exposure_analysis_job_id")
            .HasColumnType("uuid");

        _ = builder.Property(e => e.BarangayId).HasColumnName("barangay_id").HasColumnType("uuid");

        _ = builder.Property(e => e.CriticalFacilityId).HasColumnName("critical_facility_id").HasColumnType("uuid");

        _ = builder.Property(e => e.HazardLayerId).HasColumnName("hazard_layer_id").HasColumnType("uuid");

        _ = builder.Property(e => e.HazardType)
            .HasColumnName("hazard_type")
            .HasColumnType("varchar(100)")
            .IsRequired();

        _ = builder.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasColumnType("varchar(50)");

        _ = builder.Property(e => e.ExposedAreaHectares)
            .HasColumnName("exposed_area_hectares")
            .HasColumnType("numeric(18,4)");

        _ = builder.Property(e => e.ExposedFacilityCount)
            .HasColumnName("exposed_facility_count")
            .HasColumnType("integer")
            .IsRequired()
            .HasDefaultValueSql("0");

        _ = builder.Property(e => e.ExposedPopulation)
            .HasColumnName("exposed_population")
            .HasColumnType("integer");

        _ = builder.Property(e => e.RiskScore)
            .HasColumnName("risk_score")
            .HasColumnType("numeric(10,4)");

        _ = builder.Property(e => e.SummaryJson)
            .HasColumnName("summary_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()")
            .IsRequired();

        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValueSql("false")
            .IsRequired();

        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_exposure_summaries_plan");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_exposure_summaries_account");

        _ = builder.HasOne(e => e.ExposureAnalysisJob)
            .WithMany()
            .HasForeignKey(e => e.ExposureAnalysisJobId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_job");

        _ = builder.HasOne(e => e.Barangay)
            .WithMany()
            .HasForeignKey(e => e.BarangayId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_barangay");

        _ = builder.HasOne(e => e.CriticalFacility)
            .WithMany()
            .HasForeignKey(e => e.CriticalFacilityId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_facility");

        _ = builder.HasOne(e => e.HazardLayer)
            .WithMany()
            .HasForeignKey(e => e.HazardLayerId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_hazard");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_created_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_updated_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_exposure_summaries_deleted_by");
    }
}

