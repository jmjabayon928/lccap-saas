using Lccap.Domain.Common.Entities;
using Lccap.Domain.Entities;
using Lccap.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Infrastructure.Tests.Persistence;

public class PlanWorkspaceMappingTests
{
    private static LccapDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LccapDbContext>().UseNpgsql(
            "Host=127.0.0.1;Port=55434;Database=lccap_plan_workspace_tests;Username=x;Password=x").Options;

        return new LccapDbContext(options);
    }

    [Fact]
    public void Tables_map_to_expected_names()
    {
        using var ctx = CreateContext();

        Assert.Equal("plans", ctx.Model.FindEntityType(typeof(Plan))!.GetTableName());
        Assert.Equal("plan_sections", ctx.Model.FindEntityType(typeof(PlanSection))!.GetTableName());
        Assert.Equal("section_comments", ctx.Model.FindEntityType(typeof(SectionComment))!.GetTableName());
        Assert.Equal("file_assets", ctx.Model.FindEntityType(typeof(FileAsset))!.GetTableName());
        Assert.Equal("documents", ctx.Model.FindEntityType(typeof(Document))!.GetTableName());
        Assert.Equal("export_jobs", ctx.Model.FindEntityType(typeof(ExportJob))!.GetTableName());
        Assert.Equal("action_items", ctx.Model.FindEntityType(typeof(ActionItem))!.GetTableName());
        Assert.Equal("audit_logs", ctx.Model.FindEntityType(typeof(AuditLog))!.GetTableName());

        // Phase 2 Slice 9 notification + collaboration foundation
        Assert.Equal("notification_events", ctx.Model.FindEntityType(typeof(NotificationEvent))!.GetTableName());
        Assert.Equal("user_notifications", ctx.Model.FindEntityType(typeof(UserNotification))!.GetTableName());
        Assert.Equal("notification_templates", ctx.Model.FindEntityType(typeof(NotificationTemplate))!.GetTableName());
        Assert.Equal("collaboration_groups", ctx.Model.FindEntityType(typeof(CollaborationGroup))!.GetTableName());
        Assert.Equal(
            "collaboration_group_members",
            ctx.Model.FindEntityType(typeof(CollaborationGroupMember))!.GetTableName());
    }

    [Fact]
    public void Row_version_is_bytea_concurrency_token_for_batch2_entities()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[]
                 {
                     typeof(Plan),
                     typeof(PlanSection),
                     typeof(SectionComment),
                     typeof(FileAsset),
                     typeof(Document),
                     typeof(ExportJob),
                     typeof(ActionItem),
                     typeof(AuditLog),
                 })
        {
            var rv = ctx.Model.FindEntityType(clrType)!.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(rv);
            Assert.True(rv!.IsConcurrencyToken);
            Assert.Equal("bytea", rv.GetRelationalTypeMapping()?.StoreType);
        }
    }

    [Fact]
    public void SectionComment_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(SectionComment));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(SectionComment.AccountId))!.GetColumnName());
        Assert.Equal("plan_id", entity.FindProperty(nameof(SectionComment.PlanId))!.GetColumnName());
        Assert.Equal("section_key", entity.FindProperty(nameof(SectionComment.SectionKey))!.GetColumnName());
        Assert.Equal("comment_type", entity.FindProperty(nameof(SectionComment.CommentType))!.GetColumnName());
        Assert.Equal("comment_text", entity.FindProperty(nameof(SectionComment.CommentText))!.GetColumnName());
        Assert.Equal("created_by_user_id", entity.FindProperty(nameof(SectionComment.CreatedByUserId))!.GetColumnName());
        Assert.Equal("resolved_at_utc", entity.FindProperty(nameof(SectionComment.ResolvedAtUtc))!.GetColumnName());
        Assert.Equal("resolved_by_user_id", entity.FindProperty(nameof(SectionComment.ResolvedByUserId))!.GetColumnName());
        Assert.Equal("is_resolved", entity.FindProperty(nameof(SectionComment.IsResolved))!.GetColumnName());
        Assert.Equal("created_at_utc", entity.FindProperty(nameof(SectionComment.CreatedAtUtc))!.GetColumnName());
        Assert.Equal("updated_at_utc", entity.FindProperty(nameof(SectionComment.UpdatedAtUtc))!.GetColumnName());
        Assert.Equal("updated_by_user_id", entity.FindProperty(nameof(SectionComment.UpdatedByUserId))!.GetColumnName());
        Assert.Equal("is_deleted", entity.FindProperty(nameof(SectionComment.IsDeleted))!.GetColumnName());
        Assert.Equal("deleted_at_utc", entity.FindProperty(nameof(SectionComment.DeletedAtUtc))!.GetColumnName());
        Assert.Equal("deleted_by_user_id", entity.FindProperty(nameof(SectionComment.DeletedByUserId))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(SectionComment.RowVersion))!.GetColumnName());

        Assert.Equal("varchar(100)", entity.FindProperty(nameof(SectionComment.SectionKey))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal("varchar(80)", entity.FindProperty(nameof(SectionComment.CommentType))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal("text", entity.FindProperty(nameof(SectionComment.CommentText))!.GetRelationalTypeMapping()?.StoreType);
    }

    [Fact]
    public void Jsonb_columns_are_mapped_to_jsonb()
    {
        using var ctx = CreateContext();

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(PlanSection))!.FindProperty(nameof(PlanSection.SectionMetadataJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(FileAsset))!.FindProperty(nameof(FileAsset.MetadataJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.TagsJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.OldValuesJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.NewValuesJson))!.GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.MetadataJson))!.GetRelationalTypeMapping()?.StoreType);

        // Phase 2 Slice 9 jsonb payload/metadata
        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(NotificationEvent))!.FindProperty(nameof(NotificationEvent.PayloadJson))!
                .GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(NotificationTemplate))!.FindProperty(nameof(NotificationTemplate.MetadataJson))!
                .GetRelationalTypeMapping()?.StoreType);
    }

    [Fact]
    public void Row_version_is_concurrency_token_for_notification_and_collaboration_entities()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[]
                 {
                     typeof(NotificationEvent),
                     typeof(UserNotification),
                     typeof(NotificationTemplate),
                     typeof(CollaborationGroup),
                     typeof(CollaborationGroupMember),
                 })
        {
            var rv = ctx.Model.FindEntityType(clrType)!.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(rv);
            Assert.True(rv!.IsConcurrencyToken);
            Assert.Equal("bytea", rv.GetRelationalTypeMapping()?.StoreType);
        }
    }

    [Fact]
    public void Notification_and_collaboration_foreign_keys_have_expected_delete_behaviors()
    {
        using var ctx = CreateContext();

        var memberAccountColumn = ctx.Model.FindEntityType(typeof(CollaborationGroupMember))!
            .FindProperty(nameof(CollaborationGroupMember.AccountId))!
            .GetColumnName();
        Assert.Equal("account_id", memberAccountColumn);

        var eventFks = ctx.Model.FindEntityType(typeof(NotificationEvent))!.GetForeignKeys();
        Assert.Contains(eventFks, fk => fk.PrincipalEntityType.ClrType == typeof(Account) && fk.DeleteBehavior == DeleteBehavior.Cascade);

        var userNotifFks = ctx.Model.FindEntityType(typeof(UserNotification))!.GetForeignKeys();
        Assert.Contains(
            userNotifFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(NotificationEvent) && fk.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(
            userNotifFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(User) && fk.DeleteBehavior == DeleteBehavior.Cascade);

        var templateFks = ctx.Model.FindEntityType(typeof(NotificationTemplate))!.GetForeignKeys();
        Assert.Contains(
            templateFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(Account) && fk.DeleteBehavior == DeleteBehavior.Cascade);

        var groupFks = ctx.Model.FindEntityType(typeof(CollaborationGroup))!.GetForeignKeys();
        Assert.Contains(groupFks, fk => fk.PrincipalEntityType.ClrType == typeof(Account) && fk.DeleteBehavior == DeleteBehavior.Cascade);

        var memberFks = ctx.Model.FindEntityType(typeof(CollaborationGroupMember))!.GetForeignKeys();
        Assert.Contains(
            memberFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(CollaborationGroup) && fk.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(
            memberFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(User) && fk.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(
            memberFks,
            fk => fk.PrincipalEntityType.ClrType == typeof(Account)
                  && fk.GetConstraintName() == "fk_collaboration_group_members_account"
                  && fk.DeleteBehavior == DeleteBehavior.Cascade
                  && fk.Properties.Count == 1
                  && fk.Properties[0].Name == nameof(CollaborationGroupMember.AccountId));
    }

    [Fact]
    public void Foreign_keys_exist_with_expected_constraints()
    {
        using var ctx = CreateContext();

        var planFks = ctx.Model.FindEntityType(typeof(Plan))!.GetForeignKeys();
        Assert.Contains(planFks, fk => fk.GetConstraintName() == "fk_plans_account");

        var sectionFks = ctx.Model.FindEntityType(typeof(PlanSection))!.GetForeignKeys();
        Assert.Contains(sectionFks, fk => fk.GetConstraintName() == "fk_plan_sections_plan");
        Assert.Contains(sectionFks, fk => fk.GetConstraintName() == "fk_plan_sections_account");

        var assetFks = ctx.Model.FindEntityType(typeof(FileAsset))!.GetForeignKeys();
        Assert.Contains(assetFks, fk => fk.GetConstraintName() == "fk_file_assets_account");

        var documentFks = ctx.Model.FindEntityType(typeof(Document))!.GetForeignKeys();
        Assert.Contains(documentFks, fk => fk.GetConstraintName() == "fk_documents_plan");
        Assert.Contains(documentFks, fk => fk.GetConstraintName() == "fk_documents_file_asset");
        Assert.Contains(documentFks, fk => fk.GetConstraintName() == "fk_documents_account");

        var actionItemFks = ctx.Model.FindEntityType(typeof(ActionItem))!.GetForeignKeys();
        Assert.Contains(actionItemFks, fk => fk.GetConstraintName() == "fk_action_items_plan");
        Assert.Contains(actionItemFks, fk => fk.GetConstraintName() == "fk_action_items_account");
    }

    [Fact]
    public void Safe_filtered_and_unique_indexes_are_mapped_and_expression_indexes_are_not_faked()
    {
        using var ctx = CreateContext();

        var planIndexes = ctx.Model.FindEntityType(typeof(Plan))!.GetIndexes();
        Assert.Contains(planIndexes, i => i.GetDatabaseName() == "ix_plans_account_status" && i.GetFilter() != null);
        Assert.Contains(planIndexes, i => i.GetDatabaseName() == "ix_plans_account_years" && i.GetFilter() != null);
        Assert.DoesNotContain(planIndexes, i => i.GetDatabaseName() == "ux_plans_account_title_version");

        var sectionIndexes = ctx.Model.FindEntityType(typeof(PlanSection))!.GetIndexes();
        Assert.Contains(sectionIndexes, i => i.GetDatabaseName() == "ux_plan_sections_plan_section_key" && i.IsUnique);
        Assert.Contains(sectionIndexes, i => i.GetDatabaseName() == "ix_plan_sections_account_plan");

        var assetIndexes = ctx.Model.FindEntityType(typeof(FileAsset))!.GetIndexes();
        Assert.Contains(assetIndexes, i => i.GetDatabaseName() == "ix_file_assets_sha256");
        Assert.Contains(assetIndexes, i => i.GetDatabaseName() == "ix_file_assets_account_extension");

        var documentIndexes = ctx.Model.FindEntityType(typeof(Document))!.GetIndexes();
        Assert.Contains(documentIndexes, i => i.GetDatabaseName() == "ix_documents_account_plan");
        Assert.Contains(documentIndexes, i => i.GetDatabaseName() == "ix_documents_plan_created");
    }

    [Fact]
    public void No_duplicate_index_names_per_entity()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[] { typeof(Plan), typeof(PlanSection), typeof(FileAsset), typeof(Document) })
        {
            var names = ctx.Model.FindEntityType(clrType)!.GetIndexes()
                .Select(i => i.GetDatabaseName())
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList()!;

            Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count());
        }
    }

    [Fact]
    public void Document_and_file_asset_entities_exist_in_real_model()
    {
        using var ctx = CreateContext();

        Assert.NotNull(ctx.Model.FindEntityType(typeof(Document)));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(FileAsset)));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(ExportJob)));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(AuditLog)));
    }

    [Fact]
    public void Document_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Document));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(Document.AccountId))!.GetColumnName());
        Assert.Equal("plan_id", entity.FindProperty(nameof(Document.PlanId))!.GetColumnName());
        Assert.Equal("file_asset_id", entity.FindProperty(nameof(Document.FileAssetId))!.GetColumnName());
        Assert.Equal("category", entity.FindProperty(nameof(Document.Category))!.GetColumnName());
        Assert.Equal("tags_json", entity.FindProperty(nameof(Document.TagsJson))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(Document.RowVersion))!.GetColumnName());
    }

    [Fact]
    public void File_asset_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(FileAsset));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(FileAsset.AccountId))!.GetColumnName());
        Assert.Equal("owner_type", entity.FindProperty(nameof(FileAsset.OwnerType))!.GetColumnName());
        Assert.Equal("owner_id", entity.FindProperty(nameof(FileAsset.OwnerId))!.GetColumnName());
        Assert.Equal("original_file_name", entity.FindProperty(nameof(FileAsset.OriginalFileName))!.GetColumnName());
        Assert.Equal("stored_file_name", entity.FindProperty(nameof(FileAsset.StoredFileName))!.GetColumnName());
        Assert.Equal("stored_path", entity.FindProperty(nameof(FileAsset.StoredPath))!.GetColumnName());
        Assert.Equal("content_type", entity.FindProperty(nameof(FileAsset.ContentType))!.GetColumnName());
        Assert.Equal("file_extension", entity.FindProperty(nameof(FileAsset.FileExtension))!.GetColumnName());
        Assert.Equal("file_size_bytes", entity.FindProperty(nameof(FileAsset.FileSizeBytes))!.GetColumnName());
        Assert.Equal("metadata_json", entity.FindProperty(nameof(FileAsset.MetadataJson))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(FileAsset.RowVersion))!.GetColumnName());
    }

    [Fact]
    public void Row_version_is_concurrency_token_for_document_and_file_asset()
    {
        using var ctx = CreateContext();

        var documentRv = ctx.Model.FindEntityType(typeof(Document))!.FindProperty(nameof(Document.RowVersion));
        var fileAssetRv = ctx.Model.FindEntityType(typeof(FileAsset))!.FindProperty(nameof(FileAsset.RowVersion));

        Assert.NotNull(documentRv);
        Assert.NotNull(fileAssetRv);
        Assert.True(documentRv!.IsConcurrencyToken);
        Assert.True(fileAssetRv!.IsConcurrencyToken);

        var auditLogRv = ctx.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.RowVersion));
        Assert.NotNull(auditLogRv);
        Assert.True(auditLogRv!.IsConcurrencyToken);
    }

    [Fact]
    public void Document_to_file_asset_relationship_exists()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Document));

        Assert.NotNull(entity);
        Assert.Contains(
            entity!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(FileAsset)
                && fk.Properties.Count == 1
                && fk.Properties[0].Name == nameof(Document.FileAssetId));
    }

    [Fact]
    public void PlanSection_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(PlanSection));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(PlanSection.AccountId))!.GetColumnName());
        Assert.Equal("plan_id", entity.FindProperty(nameof(PlanSection.PlanId))!.GetColumnName());
        Assert.Equal("section_key", entity.FindProperty(nameof(PlanSection.SectionKey))!.GetColumnName());
        Assert.Equal("title", entity.FindProperty(nameof(PlanSection.Title))!.GetColumnName());
        Assert.Equal("content", entity.FindProperty(nameof(PlanSection.Content))!.GetColumnName());
        Assert.Equal("sort_order", entity.FindProperty(nameof(PlanSection.SortOrder))!.GetColumnName());
        Assert.Equal("last_edited_by_user_id", entity.FindProperty(nameof(PlanSection.LastEditedByUserId))!.GetColumnName());
        Assert.Equal("last_edited_at_utc", entity.FindProperty(nameof(PlanSection.LastEditedAtUtc))!.GetColumnName());
        Assert.Equal("section_metadata_json", entity.FindProperty(nameof(PlanSection.SectionMetadataJson))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(PlanSection.RowVersion))!.GetColumnName());
    }

    [Fact]
    public void PlanSection_row_version_is_concurrency_token()
    {
        using var ctx = CreateContext();
        var rowVersion = ctx.Model.FindEntityType(typeof(PlanSection))!.FindProperty(nameof(PlanSection.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion!.IsConcurrencyToken);
    }

    [Fact]
    public void PlanSection_to_plan_relationship_exists()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(PlanSection));

        Assert.NotNull(entity);
        Assert.Contains(
            entity!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(Plan)
                && fk.Properties.Count == 1
                && fk.Properties[0].Name == nameof(PlanSection.PlanId));
    }

    [Fact]
    public void PlanSection_unique_index_for_plan_and_section_key_exists()
    {
        using var ctx = CreateContext();
        var indexes = ctx.Model.FindEntityType(typeof(PlanSection))!.GetIndexes();

        Assert.Contains(indexes, i => i.GetDatabaseName() == "ux_plan_sections_plan_section_key" && i.IsUnique);
    }

    [Fact]
    public void ActionItem_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(ActionItem.AccountId))!.GetColumnName());
        Assert.Equal("plan_id", entity.FindProperty(nameof(ActionItem.PlanId))!.GetColumnName());
        Assert.Equal("title", entity.FindProperty(nameof(ActionItem.Title))!.GetColumnName());
        Assert.Equal("description", entity.FindProperty(nameof(ActionItem.Description))!.GetColumnName());
        Assert.Equal("action_type", entity.FindProperty(nameof(ActionItem.ActionType))!.GetColumnName());
        Assert.Equal("sector", entity.FindProperty(nameof(ActionItem.Sector))!.GetColumnName());
        Assert.Equal("responsible_office", entity.FindProperty(nameof(ActionItem.ResponsibleOffice))!.GetColumnName());
        Assert.Equal("budget_amount", entity.FindProperty(nameof(ActionItem.BudgetAmount))!.GetColumnName());
        Assert.Equal("funding_source", entity.FindProperty(nameof(ActionItem.FundingSource))!.GetColumnName());
        Assert.Equal("timeline_start_utc", entity.FindProperty(nameof(ActionItem.TimelineStartUtc))!.GetColumnName());
        Assert.Equal("timeline_end_utc", entity.FindProperty(nameof(ActionItem.TimelineEndUtc))!.GetColumnName());
        Assert.Equal("kpi", entity.FindProperty(nameof(ActionItem.Kpi))!.GetColumnName());
        Assert.Equal("priority_score", entity.FindProperty(nameof(ActionItem.PriorityScore))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(ActionItem.Status))!.GetColumnName());
        Assert.Equal("metadata_json", entity.FindProperty(nameof(ActionItem.MetadataJson))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(ActionItem.RowVersion))!.GetColumnName());
    }

    [Fact]
    public void ActionItem_row_version_is_concurrency_token()
    {
        using var ctx = CreateContext();
        var rowVersion = ctx.Model.FindEntityType(typeof(ActionItem))!.FindProperty(nameof(ActionItem.RowVersion));

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion!.IsConcurrencyToken);
    }

    [Fact]
    public void ActionItem_to_plan_relationship_exists()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ActionItem));

        Assert.NotNull(entity);
        Assert.Contains(
            entity!.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(Plan)
                && fk.Properties.Count == 1
                && fk.Properties[0].Name == nameof(ActionItem.PlanId));
    }

    [Fact]
    public void ExportJob_required_columns_are_mapped()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(ExportJob));

        Assert.NotNull(entity);
        Assert.Equal("account_id", entity!.FindProperty(nameof(ExportJob.AccountId))!.GetColumnName());
        Assert.Equal("plan_id", entity.FindProperty(nameof(ExportJob.PlanId))!.GetColumnName());
        Assert.Equal("export_type", entity.FindProperty(nameof(ExportJob.ExportType))!.GetColumnName());
        Assert.Equal("status", entity.FindProperty(nameof(ExportJob.Status))!.GetColumnName());
        Assert.Equal("file_asset_id", entity.FindProperty(nameof(ExportJob.FileAssetId))!.GetColumnName());
        Assert.Equal("options_json", entity.FindProperty(nameof(ExportJob.OptionsJson))!.GetColumnName());
        Assert.Equal("row_version", entity.FindProperty(nameof(ExportJob.RowVersion))!.GetColumnName());
    }

    [Fact]
    public void Tables_map_include_map_workspace_entities()
    {
        using var ctx = CreateContext();

        Assert.Equal("barangays", ctx.Model.FindEntityType(typeof(Barangay))!.GetTableName());
        Assert.Equal("critical_facilities", ctx.Model.FindEntityType(typeof(CriticalFacility))!.GetTableName());
        Assert.Equal("map_assets", ctx.Model.FindEntityType(typeof(MapAsset))!.GetTableName());
        Assert.Equal("map_annotations", ctx.Model.FindEntityType(typeof(MapAnnotation))!.GetTableName());
        Assert.Equal(
            "geojson_layer_features",
            ctx.Model.FindEntityType(typeof(GeoJsonLayerFeature))!.GetTableName());
    }

    [Fact]
    public void Row_version_is_concurrency_token_for_map_workspace_entities()
    {
        using var ctx = CreateContext();

        foreach (var clrType in new[]
                 {
                     typeof(Barangay),
                     typeof(CriticalFacility),
                     typeof(MapAsset),
                     typeof(MapAnnotation),
                     typeof(GeoJsonLayerFeature),
                 })
        {
            var rv = ctx.Model.FindEntityType(clrType)!.FindProperty(nameof(BaseEntity.RowVersion));
            Assert.NotNull(rv);
            Assert.True(rv!.IsConcurrencyToken);
            Assert.Equal("bytea", rv.GetRelationalTypeMapping()?.StoreType);
            Assert.Equal("row_version", rv.GetColumnName());
        }
    }

    [Fact]
    public void Map_workspace_entities_map_jsonb_columns()
    {
        using var ctx = CreateContext();

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(Barangay))!.FindProperty(nameof(Barangay.BoundaryGeoJson))!
                .GetRelationalTypeMapping()?.StoreType);
        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(Barangay))!.FindProperty(nameof(Barangay.MetadataJson))!
                .GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(CriticalFacility))!.FindProperty(nameof(CriticalFacility.MetadataJson))!
                .GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(MapAsset))!.FindProperty(nameof(MapAsset.BoundsJson))!
                .GetRelationalTypeMapping()?.StoreType);
        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(MapAsset))!.FindProperty(nameof(MapAsset.DefaultStyleJson))!
                .GetRelationalTypeMapping()?.StoreType);

        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(MapAnnotation))!.FindProperty(nameof(MapAnnotation.GeometryJson))!
                .GetRelationalTypeMapping()?.StoreType);
        Assert.Equal(
            "jsonb",
            ctx.Model.FindEntityType(typeof(MapAnnotation))!.FindProperty(nameof(MapAnnotation.StyleJson))!
                .GetRelationalTypeMapping()?.StoreType);

        var geoFeat = ctx.Model.FindEntityType(typeof(GeoJsonLayerFeature))!;
        Assert.Equal(
            "jsonb",
            geoFeat.FindProperty(nameof(GeoJsonLayerFeature.PropertiesJson))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal(
            "jsonb",
            geoFeat.FindProperty(nameof(GeoJsonLayerFeature.GeometryJson))!.GetRelationalTypeMapping()?.StoreType);
        Assert.Equal(
            "jsonb",
            geoFeat.FindProperty(nameof(GeoJsonLayerFeature.StyleJson))!.GetRelationalTypeMapping()?.StoreType);
    }

    [Fact]
    public void Map_workspace_foreign_keys_match_expected_accounts_and_cascades()
    {
        using var ctx = CreateContext();

        var barangayFks = ctx.Model.FindEntityType(typeof(Barangay))!.GetForeignKeys();
        Assert.Contains(barangayFks, fk => fk.GetConstraintName() == "fk_barangays_account");

        var cfFks = ctx.Model.FindEntityType(typeof(CriticalFacility))!.GetForeignKeys();
        Assert.Contains(cfFks, fk => fk.GetConstraintName() == "fk_critical_facilities_account");
        Assert.Contains(cfFks, fk => fk.GetConstraintName() == "fk_critical_facilities_plan");
        Assert.Contains(cfFks, fk => fk.GetConstraintName() == "fk_critical_facilities_barangay");

        var assetFks = ctx.Model.FindEntityType(typeof(MapAsset))!.GetForeignKeys();
        Assert.Contains(assetFks, fk => fk.GetConstraintName() == "fk_map_assets_account");
        Assert.Contains(assetFks, fk => fk.GetConstraintName() == "fk_map_assets_plan");
        Assert.Contains(assetFks, fk => fk.GetConstraintName() == "fk_map_assets_file_asset");

        var annFks = ctx.Model.FindEntityType(typeof(MapAnnotation))!.GetForeignKeys();
        Assert.Contains(annFks, fk => fk.GetConstraintName() == "fk_map_annotations_account");
        Assert.Contains(annFks, fk => fk.GetConstraintName() == "fk_map_annotations_map_asset");

        var featFks = ctx.Model.FindEntityType(typeof(GeoJsonLayerFeature))!.GetForeignKeys();
        Assert.Contains(featFks, fk => fk.GetConstraintName() == "fk_geojson_layer_features_account");
        Assert.Contains(featFks, fk => fk.GetConstraintName() == "fk_geojson_layer_features_map_asset");

        var mapToPlan = assetFks.Single(fk => fk.GetConstraintName() == "fk_map_assets_plan");
        Assert.Equal(DeleteBehavior.Cascade, mapToPlan.DeleteBehavior);

        var annoToAsset = annFks.Single(fk => fk.GetConstraintName() == "fk_map_annotations_map_asset");
        Assert.Equal(DeleteBehavior.Cascade, annoToAsset.DeleteBehavior);

        var cfToBrgy = cfFks.Single(fk => fk.GetConstraintName() == "fk_critical_facilities_barangay");
        Assert.Equal(DeleteBehavior.SetNull, cfToBrgy.DeleteBehavior);
    }

    [Fact]
    public void Map_assets_and_annotations_indexes_match_expected_filtered_indexes()
    {
        using var ctx = CreateContext();

        var assetIndexes = ctx.Model.FindEntityType(typeof(MapAsset))!.GetIndexes();
        Assert.Contains(
            assetIndexes,
            i => i.GetDatabaseName() == "ix_map_assets_account_plan" && i.GetFilter() != null);
        Assert.Contains(
            assetIndexes,
            i => i.GetDatabaseName() == "ix_map_assets_account_type" && i.GetFilter() != null);

        var annIndexes = ctx.Model.FindEntityType(typeof(MapAnnotation))!.GetIndexes();
        Assert.Contains(
            annIndexes,
            i => i.GetDatabaseName() == "ix_map_annotations_account_map_asset" && i.GetFilter() != null);
    }
}
