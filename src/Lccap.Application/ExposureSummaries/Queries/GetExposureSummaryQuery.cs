using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureSummaries.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureSummaries.Queries;

public sealed class GetExposureSummaryQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetExposureSummaryQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetExposureSummaryResult> Execute(
        Guid planId,
        Guid summaryId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetExposureSummaryResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetExposureSummaryResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetExposureSummaryResult.NotFound();
        }

        var summary = await _dbContext.ExposureSummaries.AsNoTracking()
            .SingleOrDefaultAsync(
                s => s.Id == summaryId
                     && s.PlanId == planId
                     && s.AccountId == accountId
                     && !s.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (summary is null)
        {
            return GetExposureSummaryResult.NotFound();
        }

        var dto = new ExposureSummaryDto(
            summary.Id,
            summary.PlanId,
            summary.ExposureAnalysisJobId,
            summary.BarangayId,
            summary.CriticalFacilityId,
            summary.HazardLayerId,
            summary.HazardType,
            summary.Severity,
            summary.ExposedAreaHectares,
            summary.ExposedFacilityCount,
            summary.ExposedPopulation,
            summary.RiskScore,
            summary.SummaryJson,
            summary.CreatedAtUtc);

        return GetExposureSummaryResult.Success(dto);
    }
}

public sealed class GetExposureSummaryResult
{
    private GetExposureSummaryResult(int statusCode, ExposureSummaryDto? summary)
    {
        StatusCode = statusCode;
        Summary = summary;
    }

    public int StatusCode { get; }

    public ExposureSummaryDto? Summary { get; }

    public static GetExposureSummaryResult Success(ExposureSummaryDto summary) => new(200, summary);

    public static GetExposureSummaryResult Unauthorized() => new(401, null);

    public static GetExposureSummaryResult Forbidden() => new(403, null);

    public static GetExposureSummaryResult NotFound() => new(404, null);
}

