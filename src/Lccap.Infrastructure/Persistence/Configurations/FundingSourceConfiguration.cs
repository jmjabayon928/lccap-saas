using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class FundingSourceConfiguration : IEntityTypeConfiguration<FundingSource>
{
    public void Configure(EntityTypeBuilder<FundingSource> builder)
    {
        _ = builder.ToTable(
            "funding_sources",
            "public",
            table =>
            {
                _ = table.HasCheckConstraint(
                    "ck_funding_sources_name_not_blank",
                    "length(trim(both from name)) > 0");
                _ = table.HasCheckConstraint(
                    "ck_funding_sources_type",
                    "(source_type)::text IN ('LGUInternal', 'NationalGovernment', 'ProvincialGovernment', 'Donor', 'NGO', 'PrivateSector', 'BankLoan', 'ClimateFund', 'Other')");
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
        _ = builder.Property(e => e.Name).HasColumnName("name").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.SourceType).HasColumnName("source_type").HasColumnType("varchar(80)").HasMaxLength(80).IsRequired();
        _ = builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        _ = builder.Property(e => e.ContactName).HasColumnName("contact_name").HasColumnType("varchar(200)").HasMaxLength(200);
        _ = builder.Property(e => e.ContactEmail).HasColumnName("contact_email").HasColumnType("varchar(255)").HasMaxLength(255);
        _ = builder.Property(e => e.WebsiteUrl).HasColumnName("website_url").HasColumnType("text");
        _ = builder.Property(e => e.MetadataJson)
            .HasColumnName("metadata_json")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        _ = builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        _ = builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id").HasColumnType("uuid");
        _ = builder.Property(e => e.IsDeleted).HasColumnName("is_deleted").HasDefaultValueSql("false").IsRequired();
        _ = builder.Property(e => e.DeletedAtUtc).HasColumnName("deleted_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.DeletedByUserId).HasColumnName("deleted_by_user_id").HasColumnType("uuid");

        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_funding_sources_account");
    }
}
