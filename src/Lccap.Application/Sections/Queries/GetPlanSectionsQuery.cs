using Lccap.Application.Common.Interfaces;

namespace Lccap.Application.Sections.Queries;

public class GetPlanSectionsQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanSectionsQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual Task<GetPlanSectionsResult> ExecuteAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return Task.FromResult(GetPlanSectionsResult.Forbidden());
        }

        var accountId = _currentUserContext.AccountId.Value;
        var planExists = _dbContext.Plans.Any(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted);
        if (!planExists)
        {
            return Task.FromResult(GetPlanSectionsResult.Missing());
        }

        var sections = _dbContext.PlanSections
            .Where(x => x.AccountId == accountId && x.PlanId == planId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(
                x => new PlanSectionItem(
                    x.Id,
                    x.PlanId,
                    x.SectionKey,
                    x.Title,
                    x.Content,
                    x.SortOrder,
                    x.LastEditedByUserId,
                    x.LastEditedAtUtc))
            .ToList();

        return Task.FromResult(GetPlanSectionsResult.Ok(sections));
    }
}

public sealed record PlanSectionItem(
    Guid Id,
    Guid PlanId,
    string SectionKey,
    string Title,
    string Content,
    int SortOrder,
    Guid? LastEditedByUserId,
    DateTimeOffset? LastEditedAtUtc);

public sealed record GetPlanSectionsResult(
    bool Success,
    bool ForbiddenAccess,
    bool NotFound,
    IReadOnlyList<PlanSectionItem> Sections)
{
    public static GetPlanSectionsResult Ok(IReadOnlyList<PlanSectionItem> sections) => new(true, false, false, sections);

    public static GetPlanSectionsResult Missing() => new(false, false, true, []);

    public static GetPlanSectionsResult Forbidden() => new(false, true, false, []);
}
