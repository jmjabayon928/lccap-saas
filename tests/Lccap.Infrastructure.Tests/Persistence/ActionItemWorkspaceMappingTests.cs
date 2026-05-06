using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class ActionItemWorkspaceMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55434;Database=lccap_plan_workspace_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Action_item_soft_delete_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Equal("is_deleted", entity!.FindProperty(nameof(ActionItem.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(ActionItem.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(ActionItem.DeletedByUserId))!.GetColumnName());
    }

    [Fact]
    public void Action_item_editable_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Equal("responsible_office", entity!.FindProperty(nameof(ActionItem.ResponsibleOffice))!.GetColumnName());
        Assert.Equal("budget_amount", entity.FindProperty(nameof(ActionItem.BudgetAmount))!.GetColumnName());
        Assert.Equal("funding_source", entity.FindProperty(nameof(ActionItem.FundingSource))!.GetColumnName());
        Assert.Equal("timeline_start_utc", entity.FindProperty(nameof(ActionItem.TimelineStartUtc))!.GetColumnName());
        Assert.Equal("timeline_end_utc", entity.FindProperty(nameof(ActionItem.TimelineEndUtc))!.GetColumnName());
        Assert.Equal("kpi", entity.FindProperty(nameof(ActionItem.Kpi))!.GetColumnName());
        Assert.Equal("priority_score", entity.FindProperty(nameof(ActionItem.PriorityScore))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(ActionItem.Status))!.GetColumnName());
        Assert.Equal("metadata_json", entity.FindProperty(nameof(ActionItem.MetadataJson))!.GetColumnName());
    }

    [Fact]
    public void Audit_log_entity_maps_for_action_audit_slice()
    {
        using var ctx = CreateContext();
        Assert.Equal("audit_logs", ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName());
    }

    [Fact]
    public void Audit_log_row_version_is_bytea_concurrency_token()
    {
        using var ctx = CreateContext();
        var rv = ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(BaseEntity.RowVersion));
        Assert.NotNull(rv);
        Assert.True(rv!.IsConcurrencyToken);
    }

    [Fact]
    public void Climate_expenditure_tag_maps_schema_columns()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ClimateExpenditureTag));
        Assert.NotNull(entity);
        Assert.Equal("climate_expenditure_tags", entity!.GetTableName());

        Assert.Equal("id", entity.FindProperty(nameof(BaseEntity.Id))!.GetColumnName());
        Assert.Equal("account_id", entity.FindProperty(nameof(ClimateExpenditureTag.AccountId))!.GetColumnName());
        Assert.Equal("uuid", entity.FindProperty(nameof(ClimateExpenditureTag.AccountId))!.GetColumnType());
        Assert.Equal("tag_code", entity.FindProperty(nameof(ClimateExpenditureTag.TagCode))!.GetColumnName());
        Assert.Equal("tag_name", entity.FindProperty(nameof(ClimateExpenditureTag.TagName))!.GetColumnName());
        Assert.Equal("tag_category", entity.FindProperty(nameof(ClimateExpenditureTag.TagCategory))!.GetColumnName());
        Assert.Equal("weight_percent", entity.FindProperty(nameof(ClimateExpenditureTag.WeightPercent))!.GetColumnName());
        Assert.Equal("numeric(5,2)", entity.FindProperty(nameof(ClimateExpenditureTag.WeightPercent))!.GetColumnType());

        Assert.Equal("metadata_json", entity.FindProperty(nameof(ClimateExpenditureTag.MetadataJson))!.GetColumnName());
        Assert.Equal("jsonb", entity.FindProperty(nameof(ClimateExpenditureTag.MetadataJson))!.GetColumnType());

        Assert.Equal("row_version", entity.FindProperty(nameof(BaseEntity.RowVersion))!.GetColumnName());
        Assert.Equal("bytea", entity.FindProperty(nameof(BaseEntity.RowVersion))!.GetColumnType());
        Assert.True(entity.FindProperty(nameof(BaseEntity.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void Funding_source_maps_schema_columns()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(FundingSource));
        Assert.NotNull(entity);
        Assert.Equal("funding_sources", entity!.GetTableName());

        Assert.Equal("account_id", entity.FindProperty(nameof(FundingSource.AccountId))!.GetColumnName());
        Assert.Equal("name", entity.FindProperty(nameof(FundingSource.Name))!.GetColumnName());
        Assert.Equal(250, entity.FindProperty(nameof(FundingSource.Name))!.GetMaxLength());
        Assert.Equal("source_type", entity.FindProperty(nameof(FundingSource.SourceType))!.GetColumnName());
        Assert.Equal(80, entity.FindProperty(nameof(FundingSource.SourceType))!.GetMaxLength());
        Assert.Equal("contact_email", entity.FindProperty(nameof(FundingSource.ContactEmail))!.GetColumnName());
        Assert.Equal(255, entity.FindProperty(nameof(FundingSource.ContactEmail))!.GetMaxLength());

        Assert.Equal("metadata_json", entity.FindProperty(nameof(FundingSource.MetadataJson))!.GetColumnName());
        Assert.Equal("jsonb", entity.FindProperty(nameof(FundingSource.MetadataJson))!.GetColumnType());
        Assert.True(entity.FindProperty(nameof(BaseEntity.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void Funding_program_maps_schema_columns_and_relationship_to_funding_source()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(FundingProgram));
        Assert.NotNull(entity);
        Assert.Equal("funding_programs", entity!.GetTableName());

        Assert.Equal("funding_source_id", entity.FindProperty(nameof(FundingProgram.FundingSourceId))!.GetColumnName());
        Assert.Equal("currency_code", entity.FindProperty(nameof(FundingProgram.CurrencyCode))!.GetColumnName());
        Assert.Equal("char(3)", entity.FindProperty(nameof(FundingProgram.CurrencyCode))!.GetColumnType());
        Assert.Equal("max_award_amount", entity.FindProperty(nameof(FundingProgram.MaxAwardAmount))!.GetColumnName());
        Assert.Equal("numeric(18,2)", entity.FindProperty(nameof(FundingProgram.MaxAwardAmount))!.GetColumnType());

        Assert.Equal("metadata_json", entity.FindProperty(nameof(FundingProgram.MetadataJson))!.GetColumnName());
        Assert.Equal("jsonb", entity.FindProperty(nameof(FundingProgram.MetadataJson))!.GetColumnType());

        var fkToSource = Assert.Single(entity.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(FundingSource));
        Assert.Equal(DeleteBehavior.Restrict, fkToSource.DeleteBehavior);
        Assert.Equal("fk_funding_programs_source", fkToSource.GetConstraintName());
    }

    [Fact]
    public void Action_funding_allocation_maps_scalar_columns_application_id_and_checks()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionFundingAllocation));
        Assert.NotNull(entity);
        Assert.Equal("action_funding_allocations", entity!.GetTableName());

        Assert.Equal("funding_application_id", entity.FindProperty(nameof(ActionFundingAllocation.FundingApplicationId))!.GetColumnName());

        var appNavs = entity.GetNavigations().Where(n => n.Name.Contains("FundingApplication", StringComparison.Ordinal));
        Assert.Empty(appNavs);

        Assert.Equal("allocated_amount", entity.FindProperty(nameof(ActionFundingAllocation.AllocatedAmount))!.GetColumnName());
        Assert.Equal("numeric(18,2)", entity.FindProperty(nameof(ActionFundingAllocation.AllocatedAmount))!.GetColumnType());

        Assert.Equal("fiscal_year", entity.FindProperty(nameof(ActionFundingAllocation.FiscalYear))!.GetColumnName());
        Assert.Equal("allocation_status", entity.FindProperty(nameof(ActionFundingAllocation.AllocationStatus))!.GetColumnName());
        Assert.Equal("currency_code", entity.FindProperty(nameof(ActionFundingAllocation.CurrencyCode))!.GetColumnName());
        Assert.Equal("char(3)", entity.FindProperty(nameof(ActionFundingAllocation.CurrencyCode))!.GetColumnType());
        Assert.True(entity.FindProperty(nameof(BaseEntity.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void Action_funding_allocation_relationships_have_expected_delete_behavior()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionFundingAllocation));
        Assert.NotNull(entity);

        var fkPlan = entity!.GetDeclaredForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(ActionFundingAllocation.PlanId));
        Assert.Equal(DeleteBehavior.Cascade, fkPlan.DeleteBehavior);
        Assert.Equal("fk_action_funding_allocations_plan", fkPlan.GetConstraintName());

        var fkAction = entity.GetDeclaredForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(ActionFundingAllocation.ActionItemId));
        Assert.Equal(DeleteBehavior.Cascade, fkAction.DeleteBehavior);
        Assert.Equal("fk_action_funding_allocations_action", fkAction.GetConstraintName());

        var fkSource = entity.GetDeclaredForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(ActionFundingAllocation.FundingSourceId));
        Assert.Equal(DeleteBehavior.Restrict, fkSource.DeleteBehavior);
        Assert.Equal("fk_action_funding_allocations_source", fkSource.GetConstraintName());

        var fkProgram = entity.GetDeclaredForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(ActionFundingAllocation.FundingProgramId));
        Assert.Equal(DeleteBehavior.SetNull, fkProgram.DeleteBehavior);
        Assert.Equal("fk_action_funding_allocations_program", fkProgram.GetConstraintName());

        var fkCcet = entity.GetDeclaredForeignKeys().Single(fk => fk.Properties.Single().Name == nameof(ActionFundingAllocation.ClimateExpenditureTagId));
        Assert.Equal(DeleteBehavior.SetNull, fkCcet.DeleteBehavior);
        Assert.Equal("fk_action_funding_allocations_ccet", fkCcet.GetConstraintName());
    }

    /// <summary>Nullable FK columns should not be surfaced as CLR non-nullable in the model snapshot.</summary>
    [Fact]
    public void Funding_application_id_property_is_optional_in_clr()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionFundingAllocation));
        Assert.NotNull(entity);

        Assert.True(entity!.FindProperty(nameof(ActionFundingAllocation.FundingApplicationId))!.IsNullable);
    }
}
