using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class BarangayConfiguration : IEntityTypeConfiguration<Barangay>
{
    public void Configure(EntityTypeBuilder<Barangay> builder)
    {
        _ = builder.ToTable(
            "barangays",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_barangays_households",
                    "(households IS NULL OR households >= 0)");
                _ = t.HasCheckConstraint(
                    "ck_barangays_land_area",
                    "(land_area_hectares IS NULL OR land_area_hectares >= 0)");
                _ = t.HasCheckConstraint(
                    "ck_barangays_latitude",
                    "(latitude IS NULL OR (latitude >= -90 AND latitude <= 90))");
                _ = t.HasCheckConstraint(
                    "ck_barangays_longitude",
                    "(longitude IS NULL OR (longitude >= -180 AND longitude <= 180))");
                _ = t.HasCheckConstraint(
                    "ck_barangays_name_not_blank",
                    "length(trim(both from name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_barangays_population",
                    "(population IS NULL OR population >= 0)");
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
        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        _ = builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
        _ = builder.Property(e => e.Latitude).HasColumnName("latitude").HasColumnType("numeric(10,7)");
        _ = builder.Property(e => e.Longitude).HasColumnName("longitude").HasColumnType("numeric(10,7)");
        _ = builder.Property(e => e.LandAreaHectares).HasColumnName("land_area_hectares").HasColumnType("numeric(18,4)");
        _ = builder.Property(e => e.Population).HasColumnName("population");
        _ = builder.Property(e => e.Households).HasColumnName("households");
        _ = builder.Property(e => e.Classification).HasColumnName("classification").HasMaxLength(80);
        _ = builder.Property(e => e.BoundaryGeoJson).HasColumnName("boundary_geojson").HasColumnType("jsonb");
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

        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_barangays_account");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_barangays_created_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_barangays_updated_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_barangays_deleted_by");
    }
}
