using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lccap.Infrastructure.Persistence.Configurations;

public sealed class ActionFundingAllocationConfiguration : IEntityTypeConfiguration<ActionFundingAllocation>
{
    public void Configure(EntityTypeBuilder<ActionFundingAllocation> builder)
    {
        _ = builder.ToTable(
            "action_funding_allocations",
            "public",
            table =>
            {
                _ = table.HasCheckConstraint(
                    "ck_action_funding_allocations_amounts",
                    "allocated_amount >= 0 AND (committed_amount IS NULL OR committed_amount >= 0) AND (released_amount IS NULL OR released_amount >= 0) AND (spent_amount IS NULL OR spent_amount >= 0)");
                _ = table.HasCheckConstraint(
                    "ck_action_funding_allocations_currency",
                    "currency_code ~ '^[A-Z]{3}$'");
                _ = table.HasCheckConstraint(
                    "ck_action_funding_allocations_status",
                    "(allocation_status)::text IN ('Planned', 'Committed', 'PartiallyReleased', 'Released', 'PartiallySpent', 'Spent', 'Cancelled')");
                _ = table.HasCheckConstraint(
                    "ck_action_funding_allocations_year",
                    "fiscal_year >= 2000 AND fiscal_year <= 2100");
                _ = table.HasCheckConstraint(
                    "ck_allocation_spent_not_exceed_allocated",
                    "spent_amount IS NULL OR allocated_amount IS NULL OR spent_amount <= allocated_amount");
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
        _ = builder.Property(e => e.ActionItemId).HasColumnName("action_item_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.FundingSourceId).HasColumnName("funding_source_id").HasColumnType("uuid").IsRequired();
        _ = builder.Property(e => e.FundingProgramId).HasColumnName("funding_program_id").HasColumnType("uuid");
        _ = builder.Property(e => e.FundingApplicationId).HasColumnName("funding_application_id").HasColumnType("uuid");
        _ = builder.Property(e => e.ClimateExpenditureTagId).HasColumnName("climate_expenditure_tag_id").HasColumnType("uuid");
        _ = builder.Property(e => e.FiscalYear).HasColumnName("fiscal_year").IsRequired();

        _ = builder.Property(e => e.AllocatedAmount)
            .HasColumnName("allocated_amount")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2)
            .IsRequired();
        _ = builder.Property(e => e.CommittedAmount)
            .HasColumnName("committed_amount")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2);
        _ = builder.Property(e => e.ReleasedAmount)
            .HasColumnName("released_amount")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2);
        _ = builder.Property(e => e.SpentAmount)
            .HasColumnName("spent_amount")
            .HasColumnType("numeric(18,2)")
            .HasPrecision(18, 2);

        _ = builder.Property(e => e.CurrencyCode)
            .HasColumnName("currency_code")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsFixedLength()
            .HasDefaultValue("PHP")
            .IsRequired();

        _ = builder.Property(e => e.AllocationStatus)
            .HasColumnName("allocation_status")
            .HasColumnType("varchar(50)")
            .HasMaxLength(50)
            .HasDefaultValue("Planned")
            .IsRequired();

        _ = builder.Property(e => e.Notes).HasColumnName("notes").HasColumnType("text");
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
            .HasConstraintName("fk_action_funding_allocations_account");
        _ = builder.HasOne(e => e.Plan).WithMany().HasForeignKey(e => e.PlanId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_action_funding_allocations_plan");
        _ = builder.HasOne(e => e.ActionItem).WithMany().HasForeignKey(e => e.ActionItemId).OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_action_funding_allocations_action");
        _ = builder.HasOne(e => e.FundingSource).WithMany().HasForeignKey(e => e.FundingSourceId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_action_funding_allocations_source");
        _ = builder.HasOne(e => e.FundingProgram).WithMany().HasForeignKey(e => e.FundingProgramId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_action_funding_allocations_program");
        _ = builder.HasOne(e => e.ClimateExpenditureTag).WithMany().HasForeignKey(e => e.ClimateExpenditureTagId).OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_action_funding_allocations_ccet");
    }
}
