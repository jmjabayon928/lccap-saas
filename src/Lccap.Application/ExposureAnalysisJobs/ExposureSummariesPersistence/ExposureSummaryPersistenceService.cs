using System.Collections.ObjectModel;
using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.ExposureAnalysisJobs.ExposureSummariesPersistence;

public sealed class ExposureSummaryPersistenceService : IExposureSummaryPersistenceService
{
    private const string PersistFailureErrorMessage = "Exposure computation results could not be persisted.";
    private const string PersistenceConcurrencyErrorMessage = "Exposure analysis job was modified during persistence.";

    private readonly ILccapDbContext _db;

    public ExposureSummaryPersistenceService(ILccapDbContext db)
    {
        _db = db;
    }

    public async Task<PersistExposureSummariesResult> PersistAsync(
        ExposureAnalysisJob job,
        ExposureComputationResult computationResult,
        Guid currentUserId,
        CancellationToken cancellationToken)
    {
        if (!computationResult.IsSuccess)
        {
            return PersistExposureSummariesResult.ValidationFailed(
                PersistFailureErrorMessage,
                new[] { "Computation result is marked as failure." });
        }

        var transaction = await BeginTransactionOrNoopAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction)
        {

            try
            {
                // Recheck job status inside the transaction to avoid race conditions.
                if (job.IsDeleted)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return PersistExposureSummariesResult.ValidationFailed(
                        PersistFailureErrorMessage,
                        new[] { "Exposure analysis job is deleted." });
                }

                if (!string.Equals(job.Status, "Running", StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return PersistExposureSummariesResult.ConcurrencyConflict(PersistenceConcurrencyErrorMessage);
                }

                var results = computationResult.Results;

                var hazardLayerIds = results
                    .Where(r => r.HazardLayerId.HasValue)
                    .Select(r => r.HazardLayerId!.Value)
                    .Distinct()
                    .ToList();

                var criticalFacilityIds = results
                    .Where(r => r.CriticalFacilityId.HasValue)
                    .Select(r => r.CriticalFacilityId!.Value)
                    .Distinct()
                    .ToList();

                var barangayIds = results
                    .Where(r => r.BarangayId.HasValue)
                    .Select(r => r.BarangayId!.Value)
                    .Distinct()
                    .ToList();

                var hazardLayers = hazardLayerIds.Count == 0
                    ? new List<HazardLayer>()
                    : await _db.HazardLayers
                        .Where(h =>
                            hazardLayerIds.Contains(h.Id) &&
                            h.AccountId == job.AccountId &&
                            h.PlanId == job.PlanId &&
                            !h.IsDeleted)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                var criticalFacilities = criticalFacilityIds.Count == 0
                    ? new List<CriticalFacility>()
                    : await _db.CriticalFacilities
                        .Where(f =>
                            criticalFacilityIds.Contains(f.Id) &&
                            f.AccountId == job.AccountId &&
                            f.PlanId == job.PlanId &&
                            !f.IsDeleted)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                var barangays = barangayIds.Count == 0
                    ? new List<Barangay>()
                    : await _db.Barangays
                        .Where(b =>
                            barangayIds.Contains(b.Id) &&
                            b.AccountId == job.AccountId &&
                            !b.IsDeleted)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

                var hazardLayerById = hazardLayers.ToDictionary(h => h.Id);
                var criticalFacilityById = criticalFacilities.ToDictionary(f => f.Id);
                var barangayById = barangays.ToDictionary(b => b.Id);

                var existingSummaries = await _db.ExposureSummaries
                    .Where(s =>
                        s.ExposureAnalysisJobId == job.Id &&
                        s.AccountId == job.AccountId &&
                        s.PlanId == job.PlanId &&
                        !s.IsDeleted)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var validationErrors = ValidateAllRows(
                    results,
                    hazardLayerById,
                    criticalFacilityById,
                    barangayById);

                if (validationErrors.Count != 0)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    return PersistExposureSummariesResult.ValidationFailed(
                        PersistFailureErrorMessage,
                        validationErrors);
                }

                // Replace-for-job semantics: delete existing rows for the job, then insert fresh rows.
                var nowUtc = DateTimeOffset.UtcNow;

                foreach (var summary in existingSummaries)
                {
                    summary.Archive(currentUserId, nowUtc);
                }

                var insertedSummaryCount = 0;

                foreach (var row in results)
                {
                    var exposureSummary = CreateExposureSummary(
                        job,
                        row,
                        hazardLayerById,
                        currentUserId,
                        nowUtc);

                    _db.ExposureSummaries.Add(exposureSummary);
                    insertedSummaryCount++;
                }

                var outputJson = BuildOutputJson(computationResult, insertedSummaryCount);

                job.MarkCompleted(outputJson, computationResult.CompletedAtUtc);

                // MarkCompleted does not clear ErrorMessage in the entity, but a completed job must not retain stale errors.
                job.ErrorMessage = null;

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

                return PersistExposureSummariesResult.Success(insertedSummaryCount);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return PersistExposureSummariesResult.ConcurrencyConflict(PersistenceConcurrencyErrorMessage);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
    }

    private async Task<ILccapDbTransaction> BeginTransactionOrNoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _db.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
            when (IsInMemoryTransactionUnsupported(ex))
        {
            // EF Core InMemory doesn't support transactions, but our persistence service is required
            // to use BeginTransactionAsync in production. For tests, treat it as a no-op boundary.
            return new NoopDbTransaction();
        }
    }

    private static bool IsInMemoryTransactionUnsupported(InvalidOperationException ex)
    {
        // The exact message comes from EF Core warning-to-exception promotion.
        return ex.Message.Contains("TransactionIgnoredWarning", StringComparison.Ordinal) &&
               ex.Message.Contains("in-memory", StringComparison.OrdinalIgnoreCase) &&
               ex.Message.Contains("Transactions are not supported", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class NoopDbTransaction : ILccapDbTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private static ExposureSummary CreateExposureSummary(
        ExposureAnalysisJob job,
        ExposureComputationServiceResultRow row,
        IReadOnlyDictionary<Guid, HazardLayer> hazardLayerById,
        Guid currentUserId,
        DateTimeOffset nowUtc)
    {
        _ = hazardLayerById.TryGetValue(row.HazardLayerId!.Value, out _); // validated earlier

        return new ExposureSummary
        {
            AccountId = job.AccountId,
            PlanId = job.PlanId,
            ExposureAnalysisJobId = job.Id,
            BarangayId = row.BarangayId,
            CriticalFacilityId = row.CriticalFacilityId,
            HazardLayerId = row.HazardLayerId,
            HazardType = row.HazardType.Trim(),
            Severity = row.Severity,
            ExposedAreaHectares = row.ExposedAreaHectares,
            ExposedFacilityCount = row.ExposedFacilityCount,
            ExposedPopulation = row.ExposedPopulation,
            RiskScore = row.RiskScore,
            SummaryJson = row.SummaryJson,
            CreatedByUserId = currentUserId,
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };
    }

    private static IReadOnlyList<string> ValidateAllRows(
        IReadOnlyList<ExposureComputationServiceResultRow> rows,
        IReadOnlyDictionary<Guid, HazardLayer> hazardLayerById,
        IReadOnlyDictionary<Guid, CriticalFacility> criticalFacilityById,
        IReadOnlyDictionary<Guid, Barangay> barangayById)
    {
        var errors = new List<string>();

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];

            // D. HazardType
            if (string.IsNullOrWhiteSpace(row.HazardType))
            {
                errors.Add($"Row {index}: HazardType is empty.");
                continue;
            }

            var trimmedHazardType = row.HazardType.Trim();

            // A. HazardLayerId must be present and exist.
            if (!row.HazardLayerId.HasValue)
            {
                errors.Add($"Row {index}: HazardLayerId is missing.");
                continue;
            }

            if (!hazardLayerById.TryGetValue(row.HazardLayerId.Value, out var hazardLayer))
            {
                errors.Add($"Row {index}: Referenced HazardLayer does not exist.");
                continue;
            }

            var trimmedHazardLayerType = hazardLayer.HazardType.Trim();
            if (!string.Equals(trimmedHazardLayerType, trimmedHazardType, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Row {index}: HazardType does not match HazardLayer.");
                continue;
            }

            // B. CriticalFacilityId must be present and exist.
            if (!row.CriticalFacilityId.HasValue)
            {
                errors.Add($"Row {index}: CriticalFacilityId is missing.");
                continue;
            }

            if (!criticalFacilityById.TryGetValue(row.CriticalFacilityId.Value, out var criticalFacility))
            {
                errors.Add($"Row {index}: Referenced CriticalFacility does not exist.");
                continue;
            }

            // C. BarangayId rules (optional).
            if (row.BarangayId.HasValue)
            {
                if (!barangayById.ContainsKey(row.BarangayId.Value))
                {
                    errors.Add($"Row {index}: Referenced Barangay does not exist.");
                    continue;
                }
            }

            if (criticalFacility.BarangayId.HasValue &&
                row.BarangayId.HasValue &&
                criticalFacility.BarangayId.Value != row.BarangayId.Value)
            {
                errors.Add($"Row {index}: BarangayId does not match CriticalFacility.");
                continue;
            }

            // E. Metrics constraints.
            if (row.ExposedFacilityCount < 0)
            {
                errors.Add($"Row {index}: ExposedFacilityCount is negative.");
                continue;
            }

            if (row.ExposedAreaHectares.HasValue && row.ExposedAreaHectares.Value < 0)
            {
                errors.Add($"Row {index}: ExposedAreaHectares is negative.");
                continue;
            }

            if (row.ExposedPopulation.HasValue && row.ExposedPopulation.Value < 0)
            {
                errors.Add($"Row {index}: ExposedPopulation is negative.");
                continue;
            }

            if (row.RiskScore.HasValue && row.RiskScore.Value < 0)
            {
                errors.Add($"Row {index}: RiskScore is negative.");
                continue;
            }

            // F. SummaryJson validation.
            var summaryJson = row.SummaryJson;
            if (summaryJson is null)
            {
                errors.Add($"Row {index}: SummaryJson is missing.");
                continue;
            }

            var root = summaryJson.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"Row {index}: SummaryJson is not a JSON object.");
                continue;
            }

            if (!root.TryGetProperty("mode", out var modeElement) ||
                modeElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(modeElement.GetString()))
            {
                errors.Add($"Row {index}: SummaryJson.mode is missing or invalid.");
                continue;
            }

            if (!root.TryGetProperty("boundaryPolicy", out var boundaryPolicyElement) ||
                boundaryPolicyElement.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(boundaryPolicyElement.GetString()))
            {
                errors.Add($"Row {index}: SummaryJson.boundaryPolicy is missing or invalid.");
                continue;
            }

            if (!root.TryGetProperty("matchedHazardFeatureIds", out var matchedHazardFeatureIdsElement) ||
                matchedHazardFeatureIdsElement.ValueKind != JsonValueKind.Array)
            {
                errors.Add($"Row {index}: SummaryJson.matchedHazardFeatureIds is missing or invalid.");
                continue;
            }

            foreach (var element in matchedHazardFeatureIdsElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String)
                {
                    errors.Add($"Row {index}: SummaryJson.matchedHazardFeatureIds contains non-string values.");
                    break;
                }
            }
        }

        return new ReadOnlyCollection<string>(errors);
    }

    private static JsonDocument BuildOutputJson(
        ExposureComputationResult computationResult,
        int insertedSummaryCount)
    {
        // Note: property names must match the persistence output contract exactly.
        var outputObject = new
        {
            engineName = computationResult.EngineName,
            engineVersion = computationResult.EngineVersion,
            computationRunId = computationResult.ComputationRunId,
            completedAtUtc = computationResult.CompletedAtUtc,
            resultCount = computationResult.Results.Count,
            warningCount = computationResult.Diagnostics.Warnings.Count,
            diagnostics = new
            {
                message = computationResult.Diagnostics.Message,
                warnings = computationResult.Diagnostics.Warnings,
                validationNotes = computationResult.Diagnostics.ValidationNotes,
                geometryFeatureCount = computationResult.Diagnostics.GeometryFeatureCount,
                barangayCount = computationResult.Diagnostics.BarangayCount,
                criticalFacilityCount = computationResult.Diagnostics.CriticalFacilityCount,
                crsDescription = computationResult.Diagnostics.CrsDescription
            },
            persistence = new
            {
                mode = "ReplaceForJob",
                persistedSummaryCount = insertedSummaryCount
            }
        };

        var bytes = JsonSerializer.SerializeToUtf8Bytes(outputObject);
        return JsonDocument.Parse(bytes);
    }
}

