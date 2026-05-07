using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.ExposureSummariesPersistence;
using Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;
using Lccap.Application.ExposureAnalysisJobs.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lccap.Application.ExposureAnalysisJobs.Commands;

public sealed class ProcessExposureAnalysisJobCommand
{
    private static readonly string NotConfiguredErrorMessage = "Exposure computation engine is not configured.";
    private static readonly string RequestBuilderFailedErrorMessage = "Exposure computation request could not be prepared.";
    private static readonly string PersistenceFailureErrorMessage = "Exposure computation results could not be persisted.";
    private static readonly string PersistenceConcurrencyErrorMessage = "Exposure analysis job was modified during persistence.";

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IExposureComputationClient _computationClient;
    private readonly IExposureComputationRequestBuilder _requestBuilder;
    private readonly IPythonExposureComputationClientAdapter _pythonAdapter;
    private readonly IOptions<PythonExposureComputationFeatureOptions> _pythonFeatureOptions;
    private readonly IExposureSummaryPersistenceService _persistenceService;

    public ProcessExposureAnalysisJobCommand(
        ILccapDbContext dbContext,
        ICurrentUserContext currentUserContext,
        IExposureComputationClient computationClient,
        IExposureComputationRequestBuilder requestBuilder,
        IPythonExposureComputationClientAdapter pythonAdapter,
        IOptions<PythonExposureComputationFeatureOptions> pythonFeatureOptions,
        IExposureSummaryPersistenceService persistenceService)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
        _computationClient = computationClient;
        _requestBuilder = requestBuilder;
        _pythonAdapter = pythonAdapter;
        _pythonFeatureOptions = pythonFeatureOptions;
        _persistenceService = persistenceService;
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

        var requestBuildResult = await _requestBuilder.BuildAsync(job, cancellationToken).ConfigureAwait(false);
        if (!requestBuildResult.IsSuccess)
        {
            job.MarkFailed(RequestBuilderFailedErrorMessage, nowUtc, userId);

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

        var request = BuildComputationRequest(job, hazardLayerId.Value);
        ExposureComputationResult computationResult;

        if (_pythonFeatureOptions.Value.Enabled)
        {
            // Python adapter expects the rich request produced by the request builder.
            computationResult = await _pythonAdapter
                .ExecuteAsync(requestBuildResult.Request!, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            computationResult = await _computationClient.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        }

        if (!computationResult.IsSuccess)
        {
            job.MarkFailed(computationResult.ErrorMessage ?? NotConfiguredErrorMessage, nowUtc, userId);
        }
        else
        {
            var persistResult = await _persistenceService
                .PersistAsync(job, computationResult, userId, cancellationToken)
                .ConfigureAwait(false);

            if (persistResult.IsConcurrencyConflict)
            {
                return ProcessExposureAnalysisJobResult.Conflict(new[] { PersistenceConcurrencyErrorMessage });
            }

            if (!persistResult.IsSuccess)
            {
                job.MarkFailed(PersistenceFailureErrorMessage, nowUtc, userId);
            }
        }

        // For persistence failures, the command marks the job failed and persists the job update here.
        // On persistence success, the persistence service already commits and calls SaveChanges.
        if (computationResult.IsSuccess)
        {
            // If persistence succeeded, job is already saved and we must avoid a second SaveChanges outside the transaction.
            if (job.Status == "Failed")
            {
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

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

