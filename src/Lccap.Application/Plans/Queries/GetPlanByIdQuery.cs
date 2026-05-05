using Lccap.Application.Common.Interfaces;
using Lccap.Application.Plans.Commands;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Queries;

public sealed class GetPlanByIdQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanByIdQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanByIdResult> Execute(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanByIdResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanByIdResult.Forbidden();
        }

        var plan = await _dbContext.Plans.SingleOrDefaultAsync(
            p => p.Id == planId && p.AccountId == _currentUserContext.AccountId.Value && !p.IsDeleted && p.Status != "Archived",
            cancellationToken);

        if (plan is null)
        {
            return GetPlanByIdResult.NotFound();
        }

        if (plan.RowVersion == null || plan.RowVersion.Length == 0)
        {
            plan.EnsureRowVersion();
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return GetPlanByIdResult.Success(new PlanDto(plan));
    }
}

public sealed class GetPlanByIdResult
{
    private GetPlanByIdResult(bool isSuccess, int statusCode, PlanDto? plan)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Plan = plan;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public PlanDto? Plan { get; }

    public static GetPlanByIdResult Success(PlanDto plan) => new(true, 200, plan);

    public static GetPlanByIdResult Unauthorized() => new(false, 401, null);

    public static GetPlanByIdResult Forbidden() => new(false, 403, null);

    public static GetPlanByIdResult NotFound() => new(false, 404, null);
}
