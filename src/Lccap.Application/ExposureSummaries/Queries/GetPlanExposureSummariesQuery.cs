using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureSummaries.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Lccap.Application.ExposureSummaries.Queries;

public sealed class GetPlanExposureSummariesQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanExposureSummariesQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanExposureSummariesResult> Execute(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanExposureSummariesResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanExposureSummariesResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetPlanExposureSummariesResult.NotFound();
        }

        var items = await _dbContext.ExposureSummaries.AsNoTracking()
            .Where(s => s.AccountId == accountId && s.PlanId == planId && !s.IsDeleted)
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => new ExposureSummaryDto(
                s.Id,
                s.PlanId,
                s.ExposureAnalysisJobId,
                s.BarangayId,
                s.CriticalFacilityId,
                s.HazardLayerId,
                s.HazardType,
                s.Severity,
                s.ExposedAreaHectares,
                s.ExposedFacilityCount,
                s.ExposedPopulation,
                s.RiskScore,
                s.SummaryJson,
                s.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return GetPlanExposureSummariesResult.Success(items);
    }
}

public sealed class GetPlanExposureSummariesResult
{
    private GetPlanExposureSummariesResult(int statusCode, IReadOnlyList<ExposureSummaryDto>? items)
    {
        StatusCode = statusCode;
        Items = items;
    }

    public int StatusCode { get; }

    public IReadOnlyList<ExposureSummaryDto>? Items { get; }

    public static GetPlanExposureSummariesResult Success(IReadOnlyList<ExposureSummaryDto> items) =>
        new(200, items);

    public static GetPlanExposureSummariesResult Unauthorized() => new(401, null);

    public static GetPlanExposureSummariesResult Forbidden() => new(403, null);

    public static GetPlanExposureSummariesResult NotFound() => new(404, null);
}

