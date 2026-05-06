using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.Queries;

public sealed class GetExposureAnalysisJobQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetExposureAnalysisJobQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetExposureAnalysisJobResult> Execute(
        Guid planId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetExposureAnalysisJobResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetExposureAnalysisJobResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetExposureAnalysisJobResult.NotFound();
        }

        var job = await _dbContext.ExposureAnalysisJobs.AsNoTracking()
            .SingleOrDefaultAsync(
                j => j.Id == jobId && j.PlanId == planId && j.AccountId == accountId && !j.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (job is null)
        {
            return GetExposureAnalysisJobResult.NotFound();
        }

        var dto = new ExposureAnalysisJobDto(
            job.Id,
            job.PlanId,
            job.Status,
            ExtractHazardLayerId(job.InputJson),
            job.ErrorMessage,
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc);

        return GetExposureAnalysisJobResult.Success(dto);
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

public sealed class GetExposureAnalysisJobResult
{
    private GetExposureAnalysisJobResult(int statusCode, ExposureAnalysisJobDto? job)
    {
        StatusCode = statusCode;
        Job = job;
    }

    public int StatusCode { get; }

    public ExposureAnalysisJobDto? Job { get; }

    public static GetExposureAnalysisJobResult Success(ExposureAnalysisJobDto job) => new(200, job);

    public static GetExposureAnalysisJobResult Unauthorized() => new(401, null);

    public static GetExposureAnalysisJobResult Forbidden() => new(403, null);

    public static GetExposureAnalysisJobResult NotFound() => new(404, null);
}

