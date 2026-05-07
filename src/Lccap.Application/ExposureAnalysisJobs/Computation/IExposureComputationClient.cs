namespace Lccap.Application.ExposureAnalysisJobs.Computation;

public interface IExposureComputationClient
{
    Task<ExposureComputationResult> ExecuteAsync(
        ExposureComputationRequest request,
        CancellationToken cancellationToken);
}

