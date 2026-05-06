using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.Commands;

public sealed class CreateExposureAnalysisJobCommand
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public CreateExposureAnalysisJobCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreateExposureAnalysisJobResult> Execute(
        Guid planId,
        CreateExposureAnalysisJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return CreateExposureAnalysisJobResult.Unauthorized();
        }

        if (request.HazardLayerId == Guid.Empty)
        {
            return CreateExposureAnalysisJobResult.ValidationFailed(new[] { "HazardLayerId is required." });
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return CreateExposureAnalysisJobResult.NotFound();
        }

        var hazardOk = await _dbContext.HazardLayers.AsNoTracking().AnyAsync(
                h => h.Id == request.HazardLayerId
                     && h.AccountId == accountId
                     && h.PlanId == planId
                     && !h.IsDeleted
                     && h.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (!hazardOk)
        {
            return CreateExposureAnalysisJobResult.NotFound();
        }

        var candidateJobs = await _dbContext.ExposureAnalysisJobs.AsNoTracking()
            .Where(j => j.AccountId == accountId && j.PlanId == planId && !j.IsDeleted)
            .Where(j => j.Status == "Queued" || j.Status == "Running")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var job in candidateJobs)
        {
            if (TryExtractHazardLayerId(job.InputJson, out var existingHazardLayerId) &&
                existingHazardLayerId == request.HazardLayerId)
            {
                return CreateExposureAnalysisJobResult.Conflict(
                    new[] { "A queued or running exposure analysis job already exists for this hazard layer." });
            }
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var inputJson = JsonDocument.Parse(
            $"{{\"hazardLayerId\":\"{request.HazardLayerId}\",\"requestedAtUtc\":\"{nowUtc:O}\",\"requestedByUserId\":\"{userId}\",\"mode\":\"BaselineExposure\"}}");

        var jobToCreate = new ExposureAnalysisJob
        {
            AccountId = accountId,
            PlanId = planId,
            Status = "Queued",
            InputJson = inputJson,
            OutputJson = null,
            ErrorMessage = null,
            StartedAtUtc = null,
            CompletedAtUtc = null,
            CreatedByUserId = userId,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null
        };

        jobToCreate.EnsureRowVersion();
        _ = _dbContext.ExposureAnalysisJobs.Add(jobToCreate);

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return CreateExposureAnalysisJobResult.Created(
            new ExposureAnalysisJobDto(
                jobToCreate.Id,
                jobToCreate.PlanId,
                jobToCreate.Status,
                request.HazardLayerId,
                jobToCreate.ErrorMessage,
                jobToCreate.CreatedAtUtc,
                jobToCreate.StartedAtUtc,
                jobToCreate.CompletedAtUtc));
    }

    private static bool TryExtractHazardLayerId(JsonDocument inputJson, out Guid hazardLayerId)
    {
        hazardLayerId = default;

        if (inputJson.RootElement.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!inputJson.RootElement.TryGetProperty("hazardLayerId", out var hazardLayerIdElement))
        {
            return false;
        }

        var hazardLayerIdString = hazardLayerIdElement.GetString();
        return hazardLayerIdString is not null && Guid.TryParse(hazardLayerIdString, out hazardLayerId);
    }
}

public sealed class CreateExposureAnalysisJobResult
{
    private CreateExposureAnalysisJobResult(int statusCode, IReadOnlyList<string>? errors, ExposureAnalysisJobDto? job)
    {
        StatusCode = statusCode;
        Errors = errors;
        Job = job;
    }

    public int StatusCode { get; }

    public IReadOnlyList<string>? Errors { get; }

    public ExposureAnalysisJobDto? Job { get; }

    public static CreateExposureAnalysisJobResult Created(ExposureAnalysisJobDto job) => new(201, null, job);

    public static CreateExposureAnalysisJobResult ValidationFailed(IReadOnlyList<string> errors) => new(400, errors, null);

    public static CreateExposureAnalysisJobResult Unauthorized() => new(401, null, null);

    public static CreateExposureAnalysisJobResult Forbidden() => new(403, null, null);

    public static CreateExposureAnalysisJobResult NotFound() => new(404, null, null);

    public static CreateExposureAnalysisJobResult Conflict(IReadOnlyList<string> errors) => new(409, errors, null);
}

