using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        _ = builder.ToTable(
            "documents",
            "public",
            t =>
            {
                _ = t.HasCheckConstraint(
                    "ck_documents_category",
                    "(category)::text IN ('Clup', 'Cdp', 'Drrm', 'HazardStudy', 'ClimateData', 'Map', 'Reference', 'Other')");
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
        _ = builder.Property(e => e.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.Title).HasColumnName("title").HasMaxLength(250);
        _ = builder.Property(e => e.Description).HasColumnName("description");
        _ = builder.Property(e => e.DocumentDate).HasColumnName("document_date").HasColumnType("date");
        _ = builder.Property(e => e.SourceAgency).HasColumnName("source_agency").HasMaxLength(200);
        _ = builder.Property(e => e.TagsJson).HasColumnName("tags_json").HasColumnType("jsonb").IsRequired().HasDefaultValueSql("'[]'::jsonb");
        _ = builder.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne(e => e.Account).WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_documents_account");
        _ = builder.HasOne(e => e.Plan).WithMany(p => p.Documents).HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_documents_plan");
        _ = builder.HasOne(e => e.FileAsset).WithMany(a => a.Documents).HasForeignKey(e => e.FileAssetId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_documents_file_asset");
        _ = builder.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_documents_uploaded_by");
        _ = builder.HasOne(e => e.CreatedByUser).WithMany().HasForeignKey(e => e.CreatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_documents_created_by");
        _ = builder.HasOne(e => e.UpdatedByUser).WithMany().HasForeignKey(e => e.UpdatedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_documents_updated_by");
        _ = builder.HasOne(e => e.DeletedByUser).WithMany().HasForeignKey(e => e.DeletedByUserId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_documents_deleted_by");

        _ = builder.HasIndex(e => new { e.AccountId, e.Category }).HasDatabaseName("ix_documents_account_category").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.AccountId, e.PlanId }).HasDatabaseName("ix_documents_account_plan").HasFilter("is_deleted = false");
        _ = builder.HasIndex(e => new { e.PlanId, e.CreatedAtUtc }).HasDatabaseName("ix_documents_plan_created").IsDescending(false, true)
            .HasFilter("is_deleted = false");
    }
}
