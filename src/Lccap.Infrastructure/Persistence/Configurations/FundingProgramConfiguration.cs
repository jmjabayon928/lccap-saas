using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class FundingProgramConfiguration : IEntityTypeConfiguration<FundingProgram>
{
    public void Configure(EntityTypeBuilder<FundingProgram> builder)
    {
        _ = builder.ToTable(
            "funding_programs",
            "public",
            table =>
            {
                _ = table.HasCheckConstraint(
                    "ck_funding_programs_currency",
                    "currency_code ~ '^[A-Z]{3}$'");
                _ = table.HasCheckConstraint(
                    "ck_funding_programs_dates",
                    "opens_at_utc IS NULL OR closes_at_utc IS NULL OR opens_at_utc <= closes_at_utc");
                _ = table.HasCheckConstraint(
                    "ck_funding_programs_max_award",
                    "max_award_amount IS NULL OR max_award_amount >= 0");
                _ = table.HasCheckConstraint(
                    "ck_funding_programs_name_not_blank",
                    "length(trim(both from name)) > 0");
                _ = table.HasCheckConstraint(
                    "ck_funding_programs_status",
                    "(status)::text IN ('Draft', 'Active', 'Closed', 'Archived')");
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
        _ = builder.Property(e => e.FundingSourceId).HasColumnName("funding_source_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.Name).HasColumnName("name").HasColumnType("varchar(250)").HasMaxLength(250).IsRequired();
        _ = builder.Property(e => e.ProgramCode).HasColumnName("program_code").HasColumnType("varchar(100)").HasMaxLength(100);
        _ = builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        _ = builder.Property(e => e.EligibleUses).HasColumnName("eligible_uses").HasColumnType("text");
        _ = builder.Property(e => e.ApplicationUrl).HasColumnName("application_url").HasColumnType("text");
        _ = builder.Property(e => e.OpensAtUtc).HasColumnName("opens_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.ClosesAtUtc).HasColumnName("closes_at_utc").HasColumnType("timestamptz");
        _ = builder.Property(e => e.MaxAwardAmount)
            .HasColumnName("max_award_amount")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2);
        _ = builder.Property(e => e.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsFixedLength()
            .HasDefaultValue("PHP")
            .IsRequired();
        _ = builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .HasDefaultValue("Active")
            .IsRequired();
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

        _ = builder.HasOne(e => e.FundingSource).WithMany().HasForeignKey(e => e.FundingSourceId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_funding_programs_source");
        _ = builder.HasOne<Account>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_funding_programs_account");
    }
}
