namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public sealed record ExposureComputationGeometryPolicy(
    bool FailOnInvalidGeoJson,
    bool FailOnUnsupportedGeometryTypes,
    bool FailOnEmptyGeometry,
    string RepairStrategy)
{
    public static ExposureComputationGeometryPolicy FailFastNoRepair()
    {
        return new ExposureComputationGeometryPolicy(
            FailOnInvalidGeoJson: true,
            FailOnUnsupportedGeometryTypes: true,
            FailOnEmptyGeometry: true,
            RepairStrategy: "None");
    }
}

