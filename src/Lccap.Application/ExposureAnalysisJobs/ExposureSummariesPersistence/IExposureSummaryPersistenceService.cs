using Lccap.Application.ExposureAnalysisJobs.Computation;
using Lccap.Domain.Entities;

namespace Lccap.Application.ExposureAnalysisJobs.ExposureSummariesPersistence;

public interface IExposureSummaryPersistenceService
{
    Task<PersistExposureSummariesResult> PersistAsync(
        ExposureAnalysisJob job,
        ExposureComputationResult computationResult,
        Guid currentUserId,
        CancellationToken cancellationToken);
}

