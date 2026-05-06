using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class GeoJsonLayerFeatureConfiguration : IEntityTypeConfiguration<GeoJsonLayerFeature>
{
    public void Configure(EntityTypeBuilder<GeoJsonLayerFeature> builder)
    {
        _ = builder.ToTable("geojson_layer_features", "public");

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
        _ = builder.Property(e => e.MapAssetId).HasColumnName("map_asset_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.FeatureId).HasColumnName("feature_id").HasMaxLength(150);
        _ = builder.Property(e => e.FeatureType).HasColumnName("feature_type").HasMaxLength(80);
        _ = builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(250);
        _ = builder.Property(e => e.PropertiesJson).HasColumnName("properties_json").HasColumnType("jsonb")
            .IsRequired().HasDefaultValueSql("'{}'::jsonb");
        _ = builder.Property(e => e.GeometryJson).HasColumnName("geometry_json").HasColumnType("jsonb").IsRequired();
        _ = builder.Property(e => e.StyleJson).HasColumnName("style_json").HasColumnType("jsonb").IsRequired()
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
            .HasConstraintName("fk_geojson_layer_features_account");
        _ = builder.HasOne(e => e.MapAsset).WithMany().HasForeignKey(e => e.MapAssetId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_geojson_layer_features_map_asset");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_geojson_layer_features_created_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_geojson_layer_features_updated_by");
        _ = builder.HasOne<User>().WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_geojson_layer_features_deleted_by");
    }
}
