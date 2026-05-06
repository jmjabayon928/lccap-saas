using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.Queries;

public sealed class GetPlanExposureAnalysisJobsQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanExposureAnalysisJobsQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanExposureAnalysisJobsResult> Execute(
        Guid planId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanExposureAnalysisJobsResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanExposureAnalysisJobsResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetPlanExposureAnalysisJobsResult.NotFound();
        }

        var jobs = await _dbContext.ExposureAnalysisJobs.AsNoTracking()
            .Where(j => j.AccountId == accountId && j.PlanId == planId && !j.IsDeleted)
            .OrderByDescending(j => j.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var items = jobs
            .Select(j => new ExposureAnalysisJobDto(
                j.Id,
                j.PlanId,
                j.Status,
                ExtractHazardLayerId(j.InputJson),
                j.ErrorMessage,
                j.CreatedAtUtc,
                j.StartedAtUtc,
                j.CompletedAtUtc))
            .ToList();

        return GetPlanExposureAnalysisJobsResult.Success(items);
    }

    private static Guid ExtractHazardLayerId(JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return Guid.Empty;
        }

        return inputJson.RootElement.TryGetProperty("hazardLayerId", out var hazardLayerIdElement)
            && hazardLayerIdElement.GetString() is { } hazardLayerIdString
            && Guid.TryParse(hazardLayerIdString, out var parsed)
            ? parsed
            : Guid.Empty;
    }
}

public sealed class GetPlanExposureAnalysisJobsResult
{
    private GetPlanExposureAnalysisJobsResult(int statusCode, IReadOnlyList<ExposureAnalysisJobDto>? items)
    {
        StatusCode = statusCode;
        Items = items;
    }

    public int StatusCode { get; }

    public IReadOnlyList<ExposureAnalysisJobDto>? Items { get; }

    public static GetPlanExposureAnalysisJobsResult Success(IReadOnlyList<ExposureAnalysisJobDto> items) =>
        new(200, items);

    public static GetPlanExposureAnalysisJobsResult Unauthorized() => new(401, null);

    public static GetPlanExposureAnalysisJobsResult Forbidden() => new(403, null);

    public static GetPlanExposureAnalysisJobsResult NotFound() => new(404, null);
}

