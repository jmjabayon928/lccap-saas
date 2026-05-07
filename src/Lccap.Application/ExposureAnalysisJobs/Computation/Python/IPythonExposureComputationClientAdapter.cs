using System.Threading;
using System.Threading.Tasks;
using Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

namespace Lccap.Application.ExposureAnalysisJobs.Computation.Python;

public interface IPythonExposureComputationClientAdapter
{
    Task<ExposureComputationResult> ExecuteAsync(
        ExposureComputationServiceRequest request,
        CancellationToken cancellationToken);
}
