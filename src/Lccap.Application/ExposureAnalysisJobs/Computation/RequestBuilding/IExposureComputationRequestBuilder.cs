using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;
using Lccap.Domain.Entities;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.RequestBuilding;

public interface IExposureComputationRequestBuilder
{
    Task<ExposureComputationRequestBuildResult> BuildAsync(
        ExposureAnalysisJob job,
        CancellationToken cancellationToken);
}

