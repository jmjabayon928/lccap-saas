using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Python;

public interface IPythonExposureComputationServiceClient
{
    Task<ExposureComputationServiceResponse> ExecuteAsync(
        ExposureComputationServiceRequest request,
        CancellationToken cancellationToken);
}

