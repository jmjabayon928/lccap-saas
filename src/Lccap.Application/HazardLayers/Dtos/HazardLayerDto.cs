using System;

namespace Lccap.Application.HazardLayers.Dtos;

public sealed record HazardLayerDto(
    Guid Id,
    Guid PlanId,
    Guid? MapAssetId,
    string Name,
    string HazardType,
    string Severity,
    string? Source,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);

