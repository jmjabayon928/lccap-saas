using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class FileAssetConfiguration : IEntityTypeConfiguration<FileAsset>
{
    public void Configure(EntityTypeBuilder<FileAsset> builder)
    {
        _ = builder.ToTable(
            "file_assets",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint("ck_file_assets_extension_not_blank", "length(trim(both from file_extension)) > 0");
                _ = t.HasCheckConstraint("ck_file_assets_owner_type_not_blank", "length(trim(both from owner_type)) > 0");
                _ = t.HasCheckConstraint("ck_file_assets_size", "file_size_bytes >= 0");
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
        _ = builder.Property(e => e.OwnerType).HasColumnName("owner_type").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.OwnerId).HasColumnName("owner_id").HasColumnType("uuid");
        _ = builder.Property(e => e.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
        _ = builder.Property(e => e.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(255).IsRequired();
        _ = builder.Property(e => e.StoredPath).HasColumnName("stored_path").IsRequired();
        _ = builder.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(150).IsRequired();
        _ = builder.Property(e => e.FileExtension).HasColumnName("file_extension").HasMaxLength(20).IsRequired();
        _ = builder.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes").HasColumnType("bigint").IsRequired();
        _ = builder.Property(e => e.Sha256Hash).HasColumnName("sha256_hash").HasMaxLength(128);
        _ = builder.Property(e => e.StorageProvider).HasColumnName("storage_provider").HasMaxLength(50).IsRequired().HasDefaultValue("Local");
        _ = builder.Property(e => e.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb").IsRequired()
            .HasDefaultValueSql("'{}'::jsonb");
        _ = builder.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account).WithMany(a => a.FileAssets).HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_file_assets_account");
        _ = builder.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_file_assets_uploaded_by");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_file_assets_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_file_assets_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_file_assets_deleted_by");

        _ = builder.HasMany(e => e.Documents)
            .WithOne(d => d.FileAsset)
            .HasForeignKey(d => d.FileAssetId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_documents_file_asset");

        _ = builder.HasIndex(e => new { e.AccountId, e.FileExtension }).HasDatabaseName("ix_file_assets_account_extension")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.AccountId, e.OwnerType, e.OwnerId }).HasDatabaseName("ix_file_assets_account_owner")
            .HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => e.Sha256Hash).HasDatabaseName("ix_file_assets_sha256").HasFilter("sha256_hash IS NOT NULL");
    }
}
