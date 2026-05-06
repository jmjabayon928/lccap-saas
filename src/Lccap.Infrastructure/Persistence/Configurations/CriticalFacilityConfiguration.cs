using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class CriticalFacilityConfiguration : IEntityTypeConfiguration<CriticalFacility>
{
    public void Configure(EntityTypeBuilder<CriticalFacility> builder)
    {
        _ = builder.ToTable(
            "critical_facilities",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_critical_facilities_capacity",
                    "(capacity IS NULL OR capacity >= 0)");
                _ = t.HasCheckConstraint(
                    "ck_critical_facilities_latitude",
                    "(latitude IS NULL OR (latitude >= -90 AND latitude <= 90))");
                _ = t.HasCheckConstraint(
                    "ck_critical_facilities_longitude",
                    "(longitude IS NULL OR (longitude >= -180 AND longitude <= 180))");
                _ = t.HasCheckConstraint(
                    "ck_critical_facilities_name_not_blank",
                    "length(trim(both from name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_critical_facilities_type",
                    "(facility_type)::text IN ('School','Hospital','EvacuationCenter','MunicipalHall','WaterFacility','RoadBridge','PowerFacility','Other')");
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
        _ = builder.Property(e => e.BarangayId).HasColumnName("barangay_id").HasColumnType("uuid");
        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.FacilityType).HasColumnName("facility_type").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.Latitude).HasColumnName("latitude").HasColumnType("numeric(10,7)");
        _ = builder.Property(e => e.Longitude).HasColumnName("longitude").HasColumnType("numeric(10,7)");
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.Capacity).HasColumnName("capacity");
        _ = builder.Property(e => e.IsEvacuationSite).HasColumnName("is_evacuation_site").HasDefaultValueSql("false")
            .IsRequired();
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_critical_facilities_plan");
        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_critical_facilities_account");
        _ = builder.HasOne(e => e.Barangay).WithMany().HasForeignKey(e => e.BarangayId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_critical_facilities_barangay");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_critical_facilities_created_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_critical_facilities_updated_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_critical_facilities_deleted_by");
    }
}
