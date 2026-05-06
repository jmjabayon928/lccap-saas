using Lccap.Api.Auth;
using Lccap.Api.Controllers;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Sections.Commands;
using Lccap.Application.Sections.Queries;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Tests.Integration;

public sealed class PlanSectionsControllerTests
{
    [Fact]
    public async Task SaveSection_SucceedsForCurrentAccountPlan()
    {
        var sectionId = Guid.NewGuid();
        var editedBy = Guid.NewGuid();
        var editedAt = DateTimeOffset.UtcNow;
        var controller = CreateController(
            saveResult: SavePlanSectionResult.Ok(sectionId, editedBy, editedAt));

        var result = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("Hazards", "Body", 1), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<SavePlanSectionResponse>(ok.Value);
        Assert.Equal(sectionId, payload.SectionId);
        Assert.Equal(editedBy, payload.LastEditedByUserId);
    }

    [Fact]
    public async Task SaveSection_WithBlankTitle_Returns400()
    {
        var controller = CreateController(saveResult: SavePlanSectionResult.ValidationError("Title is required."));

        var result = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("", "Body", 1), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveSection_WithBlankSectionKey_Returns400()
    {
        var controller = CreateController(saveResult: SavePlanSectionResult.ValidationError("Section key is required."));

        var result = await controller.Save(Guid.NewGuid(), "   ", new SavePlanSectionBody("Title", "Body", 1), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveSection_ForCrossTenantPlan_Returns404()
    {
        var controller = CreateController(saveResult: SavePlanSectionResult.Missing());

        var result = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("Hazards", "Body", 1), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetSections_ReturnsOnlyCurrentAccountPlanSections()
    {
        var planId = Guid.NewGuid();
        var sections = new[]
        {
            new PlanSectionItem(Guid.NewGuid(), planId, "hazards", "Hazards", "A", 0, Guid.NewGuid(), DateTimeOffset.UtcNow),
        };
        var controller = CreateController(getSectionsResult: GetPlanSectionsResult.Ok(sections));

        var result = await controller.GetSections(planId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<PlanSectionItem>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(planId, payload[0].PlanId);
    }

    [Fact]
    public async Task GetSections_SortsBySortOrderAscending()
    {
        var planId = Guid.NewGuid();
        var sections = new[]
        {
            new PlanSectionItem(Guid.NewGuid(), planId, "b", "B", "B", 1, null, null),
            new PlanSectionItem(Guid.NewGuid(), planId, "a", "A", "A", 0, null, null),
        }.OrderBy(x => x.SortOrder).ToArray();
        var controller = CreateController(getSectionsResult: GetPlanSectionsResult.Ok(sections));

        var result = await controller.GetSections(planId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<PlanSectionItem>>(ok.Value);
        Assert.True(payload[0].SortOrder <= payload[1].SortOrder);
    }

    [Fact]
    public async Task GetSectionByKey_ReturnsOnlySameAccountSection()
    {
        var planId = Guid.NewGuid();
        var section = new PlanSectionItem(Guid.NewGuid(), planId, "hazards", "Hazards", "Body", 0, Guid.NewGuid(), DateTimeOffset.UtcNow);
        var controller = CreateController(getByKeyResult: GetPlanSectionByKeyResult.Ok(section));

        var result = await controller.GetByKey(planId, "hazards", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<PlanSectionItem>(ok.Value);
        Assert.Equal("hazards", payload.SectionKey);
    }

    [Fact]
    public async Task CrossTenantGetSectionByKey_Returns404()
    {
        var controller = CreateController(getByKeyResult: GetPlanSectionByKeyResult.Missing());

        var result = await controller.GetByKey(Guid.NewGuid(), "hazards", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdatingExistingSection_ChangesContentAndLastEditedFields()
    {
        var sectionId = Guid.NewGuid();
        var editedBy = Guid.NewGuid();
        var editedAt = DateTimeOffset.UtcNow;
        var controller = CreateController(saveResult: SavePlanSectionResult.Ok(sectionId, editedBy, editedAt));

        var result = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("Hazards", "Updated content", 2), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<SavePlanSectionResponse>(ok.Value);
        Assert.Equal(editedBy, payload.LastEditedByUserId);
        Assert.Equal(editedAt, payload.LastEditedAtUtc);
    }

    [Fact]
    public async Task GetHistory_ReturnsHistoryEntries()
    {
        var planId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var history = new List<PlanSectionHistoryDto>
        {
            new(Guid.NewGuid(), sectionId, planId, "hazards", "PlanSectionUpdated", "Title", "Content", DateTimeOffset.UtcNow, Guid.NewGuid(), true)
        };
        var controller = CreateController(getHistoryResult: GetPlanSectionHistoryResult.Ok(history));

        var result = await controller.GetHistory(planId, "hazards", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = ok.Value?.GetType().GetProperty("history")?.GetValue(ok.Value) as List<PlanSectionHistoryDto>;
        Assert.NotNull(payload);
        Assert.Single(payload);
    }

    [Fact]
    public async Task RestoreSection_ReturnsOkOnSuccess()
    {
        var planId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        var editedBy = Guid.NewGuid();
        var editedAt = DateTimeOffset.UtcNow;
        var controller = CreateController(restoreResult: RestorePlanSectionResult.Ok(sectionId, editedBy, editedAt));

        var result = await controller.Restore(planId, "hazards", new RestorePlanSectionBody(Guid.NewGuid(), "Reason"), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsType<SavePlanSectionResponse>(ok.Value);
        Assert.Equal(sectionId, payload.SectionId);
        Assert.Equal(editedBy, payload.LastEditedByUserId);
    }

    [Fact]
    public async Task Viewer_can_read_section_history()
    {
        var controller = CreateController(
            role: WorkspaceRoles.Viewer,
            getHistoryResult: GetPlanSectionHistoryResult.Ok(new()));

        var result = await controller.GetHistory(Guid.NewGuid(), "hazards", CancellationToken.None);

        _ = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_save_section()
    {
        var controller = CreateController(role: WorkspaceRoles.Viewer);

        var result = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("T", "C", 1), CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Viewer_cannot_restore_section()
    {
        var controller = CreateController(role: WorkspaceRoles.Viewer);

        var result = await controller.Restore(Guid.NewGuid(), "hazards", new RestorePlanSectionBody(Guid.NewGuid(), "R"), CancellationToken.None);

        _ = Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Planner_can_save_and_restore_section()
    {
        var controller = CreateController(role: WorkspaceRoles.Planner);

        var saveResult = await controller.Save(Guid.NewGuid(), "hazards", new SavePlanSectionBody("T", "C", 1), CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(saveResult);

        var restoreResult = await controller.Restore(Guid.NewGuid(), "hazards", new RestorePlanSectionBody(Guid.NewGuid(), "R"), CancellationToken.None);
        _ = Assert.IsType<OkObjectResult>(restoreResult);
    }

    [Fact]
    public async Task GetSectionComments_returns_ok_for_reader()
    {
        var planId = Guid.NewGuid();
        var comments = new[]
        {
            new SectionCommentDto(
                Guid.NewGuid(),
                planId,
                "hazards",
                "General",
                "Review this section.",
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                false,
                null,
                null,
                null,
                new byte[8]),
        };

        var controller = CreateController(
            role: WorkspaceRoles.Viewer,
            getCommentsResult: GetSectionCommentsResult.Ok(comments));

        var result = await controller.GetSectionComments(planId, "hazards", true, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<SectionCommentDto>>(ok.Value);
        Assert.Single(payload);
        Assert.Equal(planId, payload[0].PlanId);
    }

    [Fact]
    public async Task CreateSectionComment_rejects_invalid_type()
    {
        var planId = Guid.NewGuid();
        var controller = CreateController(
            createCommentResult: CreateSectionCommentResult.ValidationError("Invalid comment type."));

        var result = await controller.CreateSectionComment(
            planId,
            "hazards",
            new CreateSectionCommentBody("BadType", "Text"),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSectionComment_rejects_blank_text()
    {
        var planId = Guid.NewGuid();
        var controller = CreateController(
            createCommentResult: CreateSectionCommentResult.ValidationError("Comment text is required."));

        var result = await controller.CreateSectionComment(
            planId,
            "hazards",
            new CreateSectionCommentBody("General", "   "),
            CancellationToken.None);

        _ = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateSectionComment_missing_section_returns_404()
    {
        var planId = Guid.NewGuid();
        var controller = CreateController(createCommentResult: CreateSectionCommentResult.Missing());

        var result = await controller.CreateSectionComment(
            planId,
            "missing",
            new CreateSectionCommentBody("General", "Text"),
            CancellationToken.None);

        _ = Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Resolve_reopen_and_archive_return_expected_status_codes()
    {
        var commentId = Guid.NewGuid();
        var controller = CreateController(
            resolveResult: ResolveSectionCommentResult.Ok(),
            reopenResult: ReopenSectionCommentResult.Ok(),
            archiveResult: ArchiveSectionCommentResult.Ok());

        _ = Assert.IsType<OkResult>(await controller.ResolveSectionComment(commentId, CancellationToken.None));
        _ = Assert.IsType<OkResult>(await controller.ReopenSectionComment(commentId, CancellationToken.None));
        _ = Assert.IsType<NoContentResult>(await controller.ArchiveSectionComment(commentId, CancellationToken.None));
    }

    [Fact]
    public async Task SectionComment_commands_persist_state_and_write_audit_logs()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var sectionKey = "hazards";
        var clock = new FixedClock(new DateTimeOffset(2026, 05, 06, 16, 30, 0, TimeSpan.Zero));
        var user = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        await using var db = CreateHardeningDbContext();
        db.Plans.Add(new Plan { Id = planId, AccountId = accountId, IsDeleted = false });
        db.PlanSections.Add(new PlanSection { Id = Guid.NewGuid(), AccountId = accountId, PlanId = planId, SectionKey = sectionKey, IsDeleted = false });
        await db.SaveChangesAsync();

        var create = new CreateSectionCommentCommand(db, user, clock);
        var created = await create.ExecuteAsync(new CreateSectionCommentRequest(planId, sectionKey, "General", "Needs more evidence."), CancellationToken.None);
        Assert.True(created.Success);
        Assert.NotNull(created.Comment);
        Assert.Equal(planId, created.Comment!.PlanId);
        Assert.Equal(sectionKey, created.Comment.SectionKey);
        Assert.Equal("General", created.Comment.CommentType);
        Assert.Equal("Needs more evidence.", created.Comment.CommentText);
        Assert.Equal(userId, created.Comment.CreatedByUserId);
        Assert.False(created.Comment.IsResolved);

        var persisted = await db.SectionComments.SingleAsync(c => c.Id == created.Comment.Id);
        Assert.Equal(accountId, persisted.AccountId);
        Assert.Equal(planId, persisted.PlanId);
        Assert.Equal(sectionKey, persisted.SectionKey);
        Assert.Equal("General", persisted.CommentType);
        Assert.Equal("Needs more evidence.", persisted.CommentText);
        Assert.Equal(userId, persisted.CreatedByUserId);
        Assert.False(persisted.IsResolved);

        Assert.Contains(
            await db.AuditLogs.ToListAsync(),
            a => a.Action == "SectionCommentCreated"
                && a.EntityName == "SectionComment"
                && a.EntityId == persisted.Id
                && a.AccountId == accountId
                && a.UserId == userId);

        var resolve = new ResolveSectionCommentCommand(db, user, clock);
        _ = await resolve.ExecuteAsync(new ResolveSectionCommentRequest(persisted.Id), CancellationToken.None);
        var resolved = await db.SectionComments.SingleAsync(c => c.Id == persisted.Id);
        Assert.True(resolved.IsResolved);
        Assert.NotNull(resolved.ResolvedAtUtc);
        Assert.Equal(userId, resolved.ResolvedByUserId);
        Assert.Equal(userId, resolved.UpdatedByUserId);

        Assert.Contains(
            await db.AuditLogs.ToListAsync(),
            a => a.Action == "SectionCommentResolved" && a.EntityId == resolved.Id);

        var reopen = new ReopenSectionCommentCommand(db, user, clock);
        _ = await reopen.ExecuteAsync(new ReopenSectionCommentRequest(resolved.Id), CancellationToken.None);
        var reopened = await db.SectionComments.SingleAsync(c => c.Id == resolved.Id);
        Assert.False(reopened.IsResolved);
        Assert.Null(reopened.ResolvedAtUtc);
        Assert.Null(reopened.ResolvedByUserId);

        Assert.Contains(
            await db.AuditLogs.ToListAsync(),
            a => a.Action == "SectionCommentReopened" && a.EntityId == reopened.Id);

        var archive = new ArchiveSectionCommentCommand(db, user, clock);
        _ = await archive.ExecuteAsync(new ArchiveSectionCommentRequest(reopened.Id), CancellationToken.None);
        var archived = await db.SectionComments.SingleAsync(c => c.Id == reopened.Id);
        Assert.True(archived.IsDeleted);
        Assert.NotNull(archived.DeletedAtUtc);
        Assert.Equal(userId, archived.DeletedByUserId);

        Assert.Contains(
            await db.AuditLogs.ToListAsync(),
            a => a.Action == "SectionCommentArchived" && a.EntityId == archived.Id);
    }

    [Fact]
    public async Task GetSectionCommentsQuery_excludes_deleted_and_sorts_unresolved_then_newest()
    {
        var accountId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var sectionKey = "hazards";
        var clock = new FixedClock(new DateTimeOffset(2026, 05, 06, 16, 31, 0, TimeSpan.Zero));
        var user = new TestCurrentUserContext(accountId, userId, true, WorkspaceRoles.Admin);

        await using var db = CreateHardeningDbContext();
        db.Plans.Add(new Plan { Id = planId, AccountId = accountId, IsDeleted = false });
        db.PlanSections.Add(new PlanSection { Id = Guid.NewGuid(), AccountId = accountId, PlanId = planId, SectionKey = sectionKey, IsDeleted = false });

        db.SectionComments.AddRange(
            new SectionComment
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = planId,
                SectionKey = sectionKey,
                CommentType = "General",
                CommentText = "Unresolved older",
                CreatedByUserId = userId,
                CreatedAtUtc = clock.UtcNow.AddMinutes(-10),
                IsResolved = false,
                IsDeleted = false
            },
            new SectionComment
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = planId,
                SectionKey = sectionKey,
                CommentType = "Validation",
                CommentText = "Resolved newest",
                CreatedByUserId = userId,
                CreatedAtUtc = clock.UtcNow.AddMinutes(-1),
                IsResolved = true,
                ResolvedAtUtc = clock.UtcNow.AddMinutes(-1),
                ResolvedByUserId = userId,
                IsDeleted = false
            },
            new SectionComment
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = planId,
                SectionKey = sectionKey,
                CommentType = "DataGap",
                CommentText = "Unresolved newest",
                CreatedByUserId = userId,
                CreatedAtUtc = clock.UtcNow,
                IsResolved = false,
                IsDeleted = false
            },
            new SectionComment
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = planId,
                SectionKey = sectionKey,
                CommentType = "General",
                CommentText = "Deleted should not show",
                CreatedByUserId = userId,
                CreatedAtUtc = clock.UtcNow,
                IsResolved = false,
                IsDeleted = true
            });

        await db.SaveChangesAsync();

        var query = new GetSectionCommentsQuery(db, user);
        var result = await query.ExecuteAsync(planId, sectionKey, includeResolved: true, CancellationToken.None);
        Assert.True(result.Success);

        Assert.Equal(3, result.Comments.Count);
        Assert.False(result.Comments[0].IsResolved);
        Assert.False(result.Comments[1].IsResolved);
        Assert.True(result.Comments[2].IsResolved);
        Assert.Equal("Unresolved newest", result.Comments[0].CommentText);
        Assert.Equal("Unresolved older", result.Comments[1].CommentText);
    }

    private static HardeningDbContext CreateHardeningDbContext()
    {
        var options = new DbContextOptionsBuilder<HardeningDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new HardeningDbContext(options);
    }

    private sealed class HardeningDbContext : DbContext, ILccapDbContext
    {
        public HardeningDbContext(DbContextOptions<HardeningDbContext> options)
            : base(options)
        {
        }

        public DbSet<Plan> Plans => Set<Plan>();
        public DbSet<PlanSection> PlanSections => Set<PlanSection>();
        public DbSet<SectionComment> SectionComments => Set<SectionComment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Not needed for these hardening tests; keep null to avoid pulling extra entity types into the in-memory model.
        public DbSet<ActionItem> ActionItems => null!;
        public DbSet<MonitoringIndicator> MonitoringIndicators => null!;
        public DbSet<MonitoringUpdate> MonitoringUpdates => null!;
        public DbSet<FileAsset> FileAssets => null!;
        public DbSet<Document> Documents => null!;
        public DbSet<ExportJob> ExportJobs => null!;
        public DbSet<RefreshToken> RefreshTokens => null!;
        public DbSet<ClimateExpenditureTag> ClimateExpenditureTags => null!;
        public DbSet<FundingSource> FundingSources => null!;
        public DbSet<FundingProgram> FundingPrograms => null!;
        public DbSet<ActionFundingAllocation> ActionFundingAllocations => null!;
        public DbSet<Barangay> Barangays => null!;
        public DbSet<CriticalFacility> CriticalFacilities => null!;
        public DbSet<MapAsset> MapAssets => null!;
        public DbSet<MapAnnotation> MapAnnotations => null!;
        public DbSet<GeoJsonLayerFeature> GeoJsonLayerFeatures => null!;

        public DbSet<User> Users => null!;

        public DbSet<NotificationEvent> NotificationEvents => null!;
        public DbSet<UserNotification> UserNotifications => null!;
        public DbSet<NotificationTemplate> NotificationTemplates => null!;

        public DbSet<CollaborationGroup> CollaborationGroups => null!;
        public DbSet<CollaborationGroupMember> CollaborationGroupMembers => null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // In-memory provider cannot materialize System.Text.Json.JsonDocument mapped properties.
            _ = modelBuilder.Ignore<System.Text.Json.JsonDocument>();
            _ = modelBuilder.Entity<PlanSection>().Ignore(e => e.SectionMetadataJson);
            _ = modelBuilder.Entity<AuditLog>().Ignore(e => e.OldValuesJson);
            _ = modelBuilder.Entity<AuditLog>().Ignore(e => e.NewValuesJson);
            _ = modelBuilder.Entity<AuditLog>().Ignore(e => e.MetadataJson);

            // Keep the model minimal: commands/tests only need scalar fields.
            _ = modelBuilder.Ignore<Account>();
            _ = modelBuilder.Ignore<User>();
            modelBuilder.Entity<Plan>().Ignore("Account");
            modelBuilder.Entity<Plan>().Ignore("CreatedByUser");
            modelBuilder.Entity<Plan>().Ignore("UpdatedByUser");
            modelBuilder.Entity<Plan>().Ignore("DeletedByUser");
            modelBuilder.Entity<Plan>().Ignore("Sections");
            modelBuilder.Entity<Plan>().Ignore("Documents");
            modelBuilder.Entity<PlanSection>().Ignore("Plan");
            modelBuilder.Entity<PlanSection>().Ignore("LastEditedByUser");
            modelBuilder.Entity<PlanSection>().Ignore("CreatedByUser");
            modelBuilder.Entity<PlanSection>().Ignore("UpdatedByUser");
            modelBuilder.Entity<PlanSection>().Ignore("DeletedByUser");
            modelBuilder.Entity<SectionComment>().Ignore("Plan");
            modelBuilder.Entity<AuditLog>().Ignore("Account");
            modelBuilder.Entity<AuditLog>().Ignore("User");
        }
    }

    private sealed class FixedClock : IClock
    {
        public FixedClock(DateTimeOffset now) => UtcNow = now;
        public DateTimeOffset UtcNow { get; }
    }

    private static PlanSectionsController CreateController(
        SavePlanSectionResult? saveResult = null,
        GetPlanSectionsResult? getSectionsResult = null,
        GetPlanSectionByKeyResult? getByKeyResult = null,
        GetPlanSectionHistoryResult? getHistoryResult = null,
        RestorePlanSectionResult? restoreResult = null,
        GetSectionCommentsResult? getCommentsResult = null,
        CreateSectionCommentResult? createCommentResult = null,
        ResolveSectionCommentResult? resolveResult = null,
        ReopenSectionCommentResult? reopenResult = null,
        ArchiveSectionCommentResult? archiveResult = null,
        string role = WorkspaceRoles.Admin)
    {
        var ctx = new TestCurrentUserContext(Guid.NewGuid(), Guid.NewGuid(), true, role);
        return new PlanSectionsController(
            new FakeSavePlanSectionCommand(saveResult ?? SavePlanSectionResult.Ok(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow)),
            new FakeGetPlanSectionsQuery(getSectionsResult ?? GetPlanSectionsResult.Ok([])),
            new FakeGetPlanSectionByKeyQuery(getByKeyResult ?? GetPlanSectionByKeyResult.Missing()),
            new FakeGetPlanSectionHistoryQuery(getHistoryResult ?? GetPlanSectionHistoryResult.Ok(new())),
            new FakeRestorePlanSectionCommand(restoreResult ?? RestorePlanSectionResult.Ok(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow)),
            new FakeGetSectionCommentsQuery(getCommentsResult ?? GetSectionCommentsResult.Ok([])),
            new FakeCreateSectionCommentCommand(createCommentResult ?? CreateSectionCommentResult.Ok(
                new SectionCommentDto(Guid.NewGuid(), Guid.NewGuid(), "k", "General", "t", Guid.NewGuid(), DateTimeOffset.UtcNow, false, null, null, null, new byte[8]))),
            new FakeResolveSectionCommentCommand(resolveResult ?? ResolveSectionCommentResult.Ok()),
            new FakeReopenSectionCommentCommand(reopenResult ?? ReopenSectionCommentResult.Ok()),
            new FakeArchiveSectionCommentCommand(archiveResult ?? ArchiveSectionCommentResult.Ok()),
            ctx);
    }

    private sealed class FakeSavePlanSectionCommand : SavePlanSectionCommand
    {
        private readonly SavePlanSectionResult _result;

        public FakeSavePlanSectionCommand(SavePlanSectionResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<SavePlanSectionResult> ExecuteAsync(SavePlanSectionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeGetPlanSectionsQuery : GetPlanSectionsQuery
    {
        private readonly GetPlanSectionsResult _result;

        public FakeGetPlanSectionsQuery(GetPlanSectionsResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<GetPlanSectionsResult> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeGetPlanSectionByKeyQuery : GetPlanSectionByKeyQuery
    {
        private readonly GetPlanSectionByKeyResult _result;

        public FakeGetPlanSectionByKeyQuery(GetPlanSectionByKeyResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<GetPlanSectionByKeyResult> ExecuteAsync(Guid planId, string sectionKey, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeGetPlanSectionHistoryQuery : GetPlanSectionHistoryQuery
    {
        private readonly GetPlanSectionHistoryResult _result;

        public FakeGetPlanSectionHistoryQuery(GetPlanSectionHistoryResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<GetPlanSectionHistoryResult> ExecuteAsync(Guid planId, string sectionKey, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeRestorePlanSectionCommand : RestorePlanSectionCommand
    {
        private readonly RestorePlanSectionResult _result;

        public FakeRestorePlanSectionCommand(RestorePlanSectionResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<RestorePlanSectionResult> ExecuteAsync(RestorePlanSectionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeGetSectionCommentsQuery : GetSectionCommentsQuery
    {
        private readonly GetSectionCommentsResult _result;

        public FakeGetSectionCommentsQuery(GetSectionCommentsResult result)
            : base(null!, null!)
        {
            _result = result;
        }

        public override Task<GetSectionCommentsResult> ExecuteAsync(
            Guid planId,
            string sectionKey,
            bool includeResolved = true,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeCreateSectionCommentCommand : CreateSectionCommentCommand
    {
        private readonly CreateSectionCommentResult _result;

        public FakeCreateSectionCommentCommand(CreateSectionCommentResult result)
            : base(null!, null!, null!)
        {
            _result = result;
        }

        public override Task<CreateSectionCommentResult> ExecuteAsync(
            CreateSectionCommentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeResolveSectionCommentCommand : ResolveSectionCommentCommand
    {
        private readonly ResolveSectionCommentResult _result;

        public FakeResolveSectionCommentCommand(ResolveSectionCommentResult result)
            : base(null!, null!, null!)
        {
            _result = result;
        }

        public override Task<ResolveSectionCommentResult> ExecuteAsync(
            ResolveSectionCommentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeReopenSectionCommentCommand : ReopenSectionCommentCommand
    {
        private readonly ReopenSectionCommentResult _result;

        public FakeReopenSectionCommentCommand(ReopenSectionCommentResult result)
            : base(null!, null!, null!)
        {
            _result = result;
        }

        public override Task<ReopenSectionCommentResult> ExecuteAsync(
            ReopenSectionCommentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class FakeArchiveSectionCommentCommand : ArchiveSectionCommentCommand
    {
        private readonly ArchiveSectionCommentResult _result;

        public FakeArchiveSectionCommentCommand(ArchiveSectionCommentResult result)
            : base(null!, null!, null!)
        {
            _result = result;
        }

        public override Task<ArchiveSectionCommentResult> ExecuteAsync(
            ArchiveSectionCommentRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_result);
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public TestCurrentUserContext(Guid? accountId, Guid? userId, bool isAuthenticated, string? role = null)
        {
            AccountId = accountId;
            UserId = userId;
            IsAuthenticated = isAuthenticated;
            Role = role;
        }

        public Guid? UserId { get; }

        public Guid? AccountId { get; }

        public string? Role { get; }

        public bool IsAuthenticated { get; }
    }
}
