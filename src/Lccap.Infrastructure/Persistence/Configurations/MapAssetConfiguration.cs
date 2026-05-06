using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class MapAssetConfiguration : IEntityTypeConfiguration<MapAsset>
{
    public void Configure(EntityTypeBuilder<MapAsset> builder)
    {
        _ = builder.ToTable(
            "map_assets",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_map_assets_format",
                    "(map_format)::text IN ('Image','Pdf','GeoJson')");
                _ = t.HasCheckConstraint(
                    "ck_map_assets_name_not_blank",
                    "length(trim(both from name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_map_assets_type",
                    "(map_type)::text IN ('Flood','Landslide','StormSurge','Boundary','LandUse','Hazard','Other')");
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
        _ = builder.Property(e => e.FileAssetId).HasColumnName("file_asset_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.MapType).HasColumnName("map_type").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.MapFormat).HasColumnName("map_format").HasMaxLength(50).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.BoundsJson).HasColumnName("bounds_json").HasColumnType("jsonb");
        _ = builder.Property(e => e.DefaultStyleJson).HasColumnName("default_style_json").HasColumnType("jsonb")
            .IsRequired().HasDefaultValueSql("'{}'::jsonb");
        _ = builder.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_map_assets_plan");
        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_map_assets_account");
        _ = builder.HasOne(e => e.FileAsset).WithMany().HasForeignKey(e => e.FileAssetId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_map_assets_file_asset");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_map_assets_uploaded_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_map_assets_created_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_map_assets_updated_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_map_assets_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.PlanId }).HasDatabaseName("ix_map_assets_account_plan")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.AccountId, e.MapType }).HasDatabaseName("ix_map_assets_account_type")
            .HasFilter("is_deleted = false");
    }
}
