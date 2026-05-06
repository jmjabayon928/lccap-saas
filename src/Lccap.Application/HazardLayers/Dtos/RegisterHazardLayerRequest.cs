namespace Lccap.Application.HazardLayers.Dtos;

public sealed record RegisterHazardLayerRequest(
    Guid MapAssetId,
    string Name,
    string HazardType,
    string Severity,
    string? Source,
    string? Description);

