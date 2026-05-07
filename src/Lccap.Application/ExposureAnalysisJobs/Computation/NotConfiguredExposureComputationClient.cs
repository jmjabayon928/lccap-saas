namespace Lccap.Application.ExposureAnalysisJobs.Computation;

public sealed class NotConfiguredExposureComputationClient : IExposureComputationClient
{
    private const string ErrorMessage = "Exposure computation engine is not configured.";

    public Task<ExposureComputationResult> ExecuteAsync(
        ExposureComputationRequest request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;

        var result = ExposureComputationResult.Failed(
            errorMessage: ErrorMessage,
            engineName: "NotConfigured",
            engineVersion: null,
            completedAtUtc: nowUtc);

        return Task.FromResult(result);
    }
}

