using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.Commands;

public sealed class ProcessExposureAnalysisJobCommand
{
    private static readonly string NotConfiguredErrorMessage = "Exposure computation engine is not configured.";

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IExposureComputationClient _computationClient;

    public ProcessExposureAnalysisJobCommand(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IExposureComputationClient computationClient)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _computationClient = computationClient;
    }

    public async Task<ProcessExposureAnalysisJobResult> Execute(
        Guid planId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return ProcessExposureAnalysisJobResult.Unauthorized();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return ProcessExposureAnalysisJobResult.NotFound();
        }

        var job = await _dbContext.ExposureAnalysisJobs
            .SingleOrDefaultAsync(
                j => j.Id == jobId && j.PlanId == planId && j.AccountId == accountId && !j.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (job is null)
        {
            return ProcessExposureAnalysisJobResult.NotFound();
        }

        if (job.Status != "Queued")
        {
            return ProcessExposureAnalysisJobResult.Conflict(new[] { "Exposure job is not in Queued state." });
        }

        var hazardLayerId = ExtractHazardLayerId(job.InputJson);
        if (hazardLayerId is null)
        {
            return ProcessExposureAnalysisJobResult.ValidationFailed(new[] { "HazardLayerId is missing from job input." });
        }

        var nowUtc = DateTimeOffset.UtcNow;
        job.MarkRunning(nowUtc, userId);

        var request = BuildComputationRequest(job, hazardLayerId.Value);
        var computationResult = await _computationClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        if (!computationResult.IsSuccess)
        {
            job.MarkFailed(computationResult.ErrorMessage ?? NotConfiguredErrorMessage, nowUtc, userId);
        }
        else
        {
            job.MarkFailed(
                computationResult.ErrorMessage ??
                "Exposure computation succeeded, but exposure summary persistence is not implemented yet.",
                nowUtc,
                userId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ProcessExposureAnalysisJobResult.Success(
            new ExposureAnalysisJobDto(
                job.Id,
                job.PlanId,
                job.Status,
                hazardLayerId.Value,
                job.ErrorMessage,
                job.CreatedAtUtc,
                job.StartedAtUtc,
                job.CompletedAtUtc));
    }

    private static ExposureComputationRequest BuildComputationRequest(
        ExposureAnalysisJob job,
        Guid hazardLayerId)
    {
        var requestedAtUtc = ExtractOptionalRequestedAtUtc(job.InputJson);
        var requestedByUserId = ExtractOptionalRequestedByUserId(job.InputJson);
        var mode = ExtractOptionalMode(job.InputJson);

        // RequestedAtUtc / RequestedByUserId / Mode are optional because input_json is future-proofed
        // and may be missing or invalid for older queued jobs.
        return new ExposureComputationRequest(
            JobId: job.Id,
            AccountId: job.AccountId,
            PlanId: job.PlanId,
            HazardLayerId: hazardLayerId,
            RequestedAtUtc: requestedAtUtc,
            RequestedByUserId: requestedByUserId,
            Mode: mode);
    }

    private static Guid? ExtractHazardLayerId(System.Text.Json.JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("hazardLayerId", out var hazardLayerIdElement))
        {
            return null;
        }

        var hazardLayerIdString = hazardLayerIdElement.GetString();
        return hazardLayerIdString is not null && Guid.TryParse(hazardLayerIdString, out var parsed)
            ? parsed
            : null;
    }

    private static DateTimeOffset? ExtractOptionalRequestedAtUtc(System.Text.Json.JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("requestedAtUtc", out var requestedAtUtcElement))
        {
            return null;
        }

        if (requestedAtUtcElement.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            return null;
        }

        var requestedAtUtcString = requestedAtUtcElement.GetString();
        return requestedAtUtcString is null ? null : DateTimeOffset.TryParse(requestedAtUtcString, out var parsed)
            ? parsed
            : null;
    }

    private static Guid? ExtractOptionalRequestedByUserId(System.Text.Json.JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("requestedByUserId", out var requestedByUserIdElement))
        {
            return null;
        }

        if (requestedByUserIdElement.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            return null;
        }

        var requestedByUserIdString = requestedByUserIdElement.GetString();
        return requestedByUserIdString is null ? null : Guid.TryParse(requestedByUserIdString, out var parsed)
            ? parsed
            : null;
    }

    private static string? ExtractOptionalMode(System.Text.Json.JsonDocument inputJson)
    {
        if (inputJson.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return null;
        }

        if (!inputJson.RootElement.TryGetProperty("mode", out var modeElement))
        {
            return null;
        }

        if (modeElement.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            return null;
        }

        var mode = modeElement.GetString();
        if (mode is null) return null;
        return mode.Trim().Length > 0 ? mode.Trim() : null;
    }
}

public sealed class ProcessExposureAnalysisJobResult
{
    private ProcessExposureAnalysisJobResult(
        int statusCode,
        IReadOnlyList<string>? errors,
        ExposureAnalysisJobDto? job)
    {
        StatusCode = statusCode;
        Errors = errors;
        Job = job;
    }

    public int StatusCode { get; }

    public IReadOnlyList<string>? Errors { get; }

    public ExposureAnalysisJobDto? Job { get; }

    public static ProcessExposureAnalysisJobResult Success(ExposureAnalysisJobDto job) => new(200, null, job);

    public static ProcessExposureAnalysisJobResult ValidationFailed(IReadOnlyList<string> errors) => new(400, errors, null);

    public static ProcessExposureAnalysisJobResult Unauthorized() => new(401, null, null);

    public static ProcessExposureAnalysisJobResult Forbidden() => new(403, null, null);

    public static ProcessExposureAnalysisJobResult NotFound() => new(404, null, null);

    public static ProcessExposureAnalysisJobResult Conflict(IReadOnlyList<string> errors) => new(409, errors, null);
}

