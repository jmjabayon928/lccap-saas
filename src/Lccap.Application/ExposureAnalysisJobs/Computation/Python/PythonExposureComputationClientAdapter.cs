using System;
using System.Threading;
using System.Threading.Tasks;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Application.ExposureAnalysisJobs.Computation.Python;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Python;

public sealed class PythonExposureComputationClientAdapter : IPythonExposureComputationClientAdapter
{
    private readonly IPythonExposureComputationServiceClient _pythonClient;

    public PythonExposureComputationClientAdapter(
        IPythonExposureComputationServiceClient pythonClient)
    {
        ArgumentNullException.ThrowIfNull(pythonClient);
        _pythonClient = pythonClient;
    }

    public async Task<ExposureComputationResult> ExecuteAsync(
        ExposureComputationServiceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _pythonClient.ExecuteAsync(
                request,
                cancellationToken)
            .ConfigureAwait(false);

        if (response.Success)
        {
            return new ExposureComputationResult(
                IsSuccess: true,
                ErrorMessage: null,
                EngineName: response.EngineName,
                EngineVersion: response.EngineVersion,
                CompletedAtUtc: response.CompletedAtUtc,
                ComputationRunId: response.ComputationRunId,
                Diagnostics: response.Diagnostics,
                Results: response.Results);
        }

        var errorCode = response.ErrorCode;
        var isEngineUnavailable =
            string.Equals(
                errorCode,
                ExposureComputationEngineErrorCode.EngineUnavailable,
                System.StringComparison.Ordinal);

        if (isEngineUnavailable)
        {
            return ExposureComputationResult.Failed(
                errorMessage: "Exposure computation engine is not configured.",
                engineName: response.EngineName,
                engineVersion: response.EngineVersion,
                completedAtUtc: response.CompletedAtUtc,
                computationRunId: response.ComputationRunId,
                diagnostics: response.Diagnostics,
                results: Array.Empty<ExposureComputationServiceResultRow>());
        }

        var isValidationFailed =
            string.Equals(
                errorCode,
                ExposureComputationEngineErrorCode.ValidationFailed,
                System.StringComparison.Ordinal);

        if (isValidationFailed)
        {
            return ExposureComputationResult.Failed(
                errorMessage: "Exposure computation service returned an invalid response.",
                engineName: response.EngineName,
                engineVersion: response.EngineVersion,
                completedAtUtc: response.CompletedAtUtc,
                computationRunId: response.ComputationRunId,
                diagnostics: response.Diagnostics,
                results: Array.Empty<ExposureComputationServiceResultRow>());
        }

        return ExposureComputationResult.Failed(
            errorMessage: "Exposure computation service returned an invalid response.",
            engineName: response.EngineName,
            engineVersion: response.EngineVersion,
            completedAtUtc: response.CompletedAtUtc,
            computationRunId: response.ComputationRunId,
            diagnostics: response.Diagnostics,
            results: Array.Empty<ExposureComputationServiceResultRow>());
    }
}
