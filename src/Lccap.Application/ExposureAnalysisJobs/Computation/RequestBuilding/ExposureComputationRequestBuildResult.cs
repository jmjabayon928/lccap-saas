using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;

public sealed record ExposureComputationRequestBuildResult(
    bool IsSuccess,
    ExposureComputationServiceRequest? Request,
    string? ErrorCode,
    string? ErrorMessage,
    IReadOnlyList<string> ValidationErrors)
{
    public static ExposureComputationRequestBuildResult Success(ExposureComputationServiceRequest request) =>
        new(
            IsSuccess: true,
            Request: request,
            ErrorCode: null,
            ErrorMessage: null,
            ValidationErrors: Array.Empty<string>());

    public static ExposureComputationRequestBuildResult Failed(
        string errorCode,
        string errorMessage,
        IReadOnlyList<string>? validationErrors = null) =>
        new(
            IsSuccess: false,
            Request: null,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage,
            ValidationErrors: validationErrors ?? Array.Empty<string>());
}

