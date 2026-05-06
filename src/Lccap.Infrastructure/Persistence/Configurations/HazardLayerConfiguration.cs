using System.Text.Json;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class HazardLayerConfiguration : IEntityTypeConfiguration<HazardLayer>
{
    public void Configure(EntityTypeBuilder<HazardLayer> builder)
    {
        _ = builder.ToTable(
            "hazard_layers",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_hazard_layers_name_not_blank", "length(trim(both from name)) > 0");
                _ = t.HasCheckConstraint(
                    "ck_hazard_layers_severity",
                    "(severity)::text IN ('Low','Moderate','High','VeryHigh')");
                _ = t.HasCheckConstraint(
                    "ck_hazard_layers_type_not_blank",
                    "length(trim(both from hazard_type)) > 0");
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
        _ = builder.Property(e => e.MapAssetId).HasColumnName("map_asset_id").HasColumnType("uuid");

        _ = builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.HazardType).HasColumnName("hazard_type").HasMaxLength(100).IsRequired();
        _ = builder.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(50).IsRequired();
        _ = builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(200);
        _ = builder.Property(e => e.Description).HasColumnName("description");

        _ = builder.Property(e => e.GeometryId).HasColumnName("geometry_id").HasColumnType("uuid");

        _ = builder.Property(e => e.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");

        _ = builder.Property(e => e.IsActive).HasColumnName("is_active").HasDefaultValueSql("true").IsRequired();

        _ = builder.Property(e => e.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");

        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");

        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_hazard_layers_plan");

        _ = builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(e => e.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_hazard_layers_account");

        _ = builder.HasOne(e => e.MapAsset)
            .WithMany()
            .HasForeignKey(e => e.MapAssetId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_hazard_layers_map_asset");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_hazard_layers_created_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UpdatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_hazard_layers_updated_by");

        _ = builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.DeletedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_hazard_layers_deleted_by");
    }
}

