using System.Collections.ObjectModel;

namespace Lccap.Application.ExposureAnalysisJobs.ExposureSummariesPersistence;

public sealed class PersistExposureSummariesResult
{
    private PersistExposureSummariesResult(
        bool isSuccess,
        int persistedSummaryCount,
        string? errorMessage,
        bool isConcurrencyConflict,
        IReadOnlyList<string> validationErrors)
    {
        IsSuccess = isSuccess;
        PersistedSummaryCount = persistedSummaryCount;
        ErrorMessage = errorMessage;
        IsConcurrencyConflict = isConcurrencyConflict;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public int PersistedSummaryCount { get; }

    public string? ErrorMessage { get; }

    public bool IsConcurrencyConflict { get; }

    public IReadOnlyList<string> ValidationErrors { get; }

    public static PersistExposureSummariesResult Success(int persistedSummaryCount) =>
        new(
            isSuccess: true,
            persistedSummaryCount: persistedSummaryCount,
            errorMessage: null,
            isConcurrencyConflict: false,
            validationErrors: Array.Empty<string>());

    public static PersistExposureSummariesResult ValidationFailed(
        string errorMessage,
        IReadOnlyList<string> validationErrors) =>
        new(
            isSuccess: false,
            persistedSummaryCount: 0,
            errorMessage: errorMessage,
            isConcurrencyConflict: false,
            validationErrors: validationErrors ?? Array.Empty<string>());

    public static PersistExposureSummariesResult ConcurrencyConflict(string errorMessage) =>
        new(
            isSuccess: false,
            persistedSummaryCount: 0,
            errorMessage: errorMessage,
            isConcurrencyConflict: true,
            validationErrors: Array.Empty<string>());
}

