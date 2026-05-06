using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureSummaries.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureSummaries.Queries;

public sealed class GetJobExposureSummariesQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetJobExposureSummariesQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetJobExposureSummariesResult> Execute(
        Guid planId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetJobExposureSummariesResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetJobExposureSummariesResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetJobExposureSummariesResult.NotFound();
        }

        var jobOk = await _dbContext.ExposureAnalysisJobs.AsNoTracking().AnyAsync(
                j => j.Id == jobId && j.AccountId == accountId && j.PlanId == planId && !j.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!jobOk)
        {
            return GetJobExposureSummariesResult.NotFound();
        }

        var items = await _dbContext.ExposureSummaries.AsNoTracking()
            .Where(s => s.AccountId == accountId && s.PlanId == planId && s.ExposureAnalysisJobId == jobId && !s.IsDeleted)
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

        return GetJobExposureSummariesResult.Success(items);
    }
}

public sealed class GetJobExposureSummariesResult
{
    private GetJobExposureSummariesResult(int statusCode, IReadOnlyList<ExposureSummaryDto>? items)
    {
        StatusCode = statusCode;
        Items = items;
    }

    public int StatusCode { get; }

    public IReadOnlyList<ExposureSummaryDto>? Items { get; }

    public static GetJobExposureSummariesResult Success(IReadOnlyList<ExposureSummaryDto> items) =>
        new(200, items);

    public static GetJobExposureSummariesResult Unauthorized() => new(401, null);

    public static GetJobExposureSummariesResult Forbidden() => new(403, null);

    public static GetJobExposureSummariesResult NotFound() => new(404, null);
}

