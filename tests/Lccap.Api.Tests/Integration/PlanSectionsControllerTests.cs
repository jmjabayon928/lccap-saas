using Lccap.Api.Controllers;
using Lccap.Application.Sections.Commands;
using Lccap.Application.Sections.Queries;
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

    private static PlanSectionsController CreateController(
        SavePlanSectionResult? saveResult = null,
        GetPlanSectionsResult? getSectionsResult = null,
        GetPlanSectionByKeyResult? getByKeyResult = null)
    {
        return new PlanSectionsController(
            new FakeSavePlanSectionCommand(saveResult ?? SavePlanSectionResult.Ok(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow)),
            new FakeGetPlanSectionsQuery(getSectionsResult ?? GetPlanSectionsResult.Ok([])),
            new FakeGetPlanSectionByKeyQuery(getByKeyResult ?? GetPlanSectionByKeyResult.Missing()));
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
}
