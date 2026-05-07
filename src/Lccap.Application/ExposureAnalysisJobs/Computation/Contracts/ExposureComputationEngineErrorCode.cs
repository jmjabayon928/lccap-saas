namespace Lccap.Application.ExposureAnalysisJobs.Computation.Contracts;

public static class ExposureComputationEngineErrorCode
{
    public const string EngineUnavailable = "EngineUnavailable";
    public const string InvalidGeoJson = "InvalidGeoJson";
    public const string CrsAmbiguous = "CrsAmbiguous";
    public const string CrsUnsupported = "CrsUnsupported";
    public const string UnsupportedGeometry = "UnsupportedGeometry";
    public const string ValidationFailed = "ValidationFailed";
    public const string ComputationTimeout = "ComputationTimeout";
    public const string PayloadTooLarge = "PayloadTooLarge";
    public const string InternalError = "InternalError";
    public const string ContractVersionMismatch = "ContractVersionMismatch";
}

