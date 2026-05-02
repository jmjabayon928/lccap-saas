using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Queries;

/// <summary>
/// List projection for GET /api/plans — excludes concurrency tokens and non-list metadata.
/// </summary>
public sealed record PlanListItemDto(
    Guid Id,
    Guid AccountId,
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed class GetPlansQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlansQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlansResult> Execute(CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlansResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlansResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var plans = await _dbContext.Plans
            .AsNoTracking()
            .Where(p => p.AccountId == accountId && !p.IsDeleted)
            .OrderByDescending(p => p.UpdatedAtUtc ?? p.CreatedAtUtc)
            .Select(p => new PlanListItemDto(
                p.Id,
                p.AccountId,
                p.Title,
                p.StartYear,
                p.EndYear,
                p.Status,
                p.TemplateMode,
                p.VersionNumber,
                p.Description,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return GetPlansResult.Success(plans);
    }
}

public sealed class GetPlansResult
{
    private GetPlansResult(bool isSuccess, int statusCode, IReadOnlyList<PlanListItemDto>? plans)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Plans = plans;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public IReadOnlyList<PlanListItemDto>? Plans { get; }

    public static GetPlansResult Success(IReadOnlyList<PlanListItemDto> plans) =>
        new(true, 200, plans);

    public static GetPlansResult Unauthorized() => new(false, 401, null);

    public static GetPlansResult Forbidden() => new(false, 403, null);
}
