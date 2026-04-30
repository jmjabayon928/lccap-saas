using Lccap.Application.Common.Interfaces;

namespace Lccap.Application.Sections.Queries;

public class GetPlanSectionByKeyQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanSectionByKeyQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual Task<GetPlanSectionByKeyResult> ExecuteAsync(
        Guid planId,
        string sectionKey,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return Task.FromResult(GetPlanSectionByKeyResult.Forbidden());
        }

        var accountId = _currentUserContext.AccountId.Value;
        var planExists = _dbContext.Plans.Any(p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted);
        if (!planExists)
        {
            return Task.FromResult(GetPlanSectionByKeyResult.Missing());
        }

        var normalizedKey = sectionKey?.Trim() ?? string.Empty;
        var section = _dbContext.PlanSections
            .Where(x => x.AccountId == accountId && x.PlanId == planId && x.SectionKey == normalizedKey && !x.IsDeleted)
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
            .SingleOrDefault();

        return section is null
            ? Task.FromResult(GetPlanSectionByKeyResult.Missing())
            : Task.FromResult(GetPlanSectionByKeyResult.Ok(section));
    }
}

public sealed record GetPlanSectionByKeyResult(
    bool Success,
    bool ForbiddenAccess,
    bool NotFound,
    PlanSectionItem? Section)
{
    public static GetPlanSectionByKeyResult Ok(PlanSectionItem section) => new(true, false, false, section);

    public static GetPlanSectionByKeyResult Missing() => new(false, false, true, null);

    public static GetPlanSectionByKeyResult Forbidden() => new(false, true, false, null);
}
