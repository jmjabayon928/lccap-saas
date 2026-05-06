using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.HazardLayers.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.HazardLayers.Commands;

public sealed class RegisterHazardLayerCommand
{
    private static readonly HashSet<string> AllowedSeverities = new(StringComparer.Ordinal)
    {
        "Low",
        "Moderate",
        "High",
        "VeryHigh"
    };

    public const int MaxNameLength = 250;
    public const int MaxHazardTypeLength = 100;
    public const int MaxSourceLength = 200;

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public RegisterHazardLayerCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<RegisterHazardLayerResult> Execute(
        Guid planId,
        RegisterHazardLayerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return RegisterHazardLayerResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.HazardType))
        {
            // Early validation to avoid query costs; remaining validation is done below.
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length == 0 || name.Length > MaxNameLength)
        {
            return RegisterHazardLayerResult.ValidationFailed(new[] { "Name is required and must be at most 250 characters." });
        }

        var hazardType = request.HazardType?.Trim() ?? string.Empty;
        if (hazardType.Length == 0 || hazardType.Length > MaxHazardTypeLength)
        {
            return RegisterHazardLayerResult.ValidationFailed(new[] { "HazardType is required and must be at most 100 characters." });
        }

        var severity = request.Severity?.Trim() ?? string.Empty;
        if (severity.Length == 0 || !AllowedSeverities.Contains(severity))
        {
            return RegisterHazardLayerResult.ValidationFailed(new[] { "Severity is invalid." });
        }

        var source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim();
        if (source is not null && source.Length > MaxSourceLength)
        {
            return RegisterHazardLayerResult.ValidationFailed(new[] { "Source must be at most 200 characters." });
        }

        var description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return RegisterHazardLayerResult.NotFound();
        }

        var mapAssetOk = await (
                from m in _dbContext.MapAssets.AsNoTracking()
                join f in _dbContext.FileAssets.AsNoTracking() on m.FileAssetId equals f.Id
                where m.Id == request.MapAssetId
                      && m.AccountId == accountId
                      && m.PlanId == planId
                      && !m.IsDeleted
                      && f.AccountId == accountId
                      && !f.IsDeleted
                      && m.MapFormat == "GeoJson"
                      && m.MapType == "Hazard"
                select m.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!mapAssetOk)
        {
            return RegisterHazardLayerResult.NotFound();
        }

        var duplicateExists = await _dbContext.HazardLayers.AsNoTracking().AnyAsync(
                h => h.AccountId == accountId
                     && h.PlanId == planId
                     && h.MapAssetId == request.MapAssetId
                     && !h.IsDeleted
                     && h.IsActive,
                cancellationToken)
            .ConfigureAwait(false);

        if (duplicateExists)
        {
            return RegisterHazardLayerResult.Conflict(new[] { "An active hazard layer already exists for this plan and map asset." });
        }

        var now = DateTimeOffset.UtcNow;
        var hazardLayer = new HazardLayer
        {
            AccountId = accountId,
            PlanId = planId,
            MapAssetId = request.MapAssetId,
            Name = name,
            HazardType = hazardType,
            Severity = severity,
            Source = source,
            Description = description,
            GeometryId = null,
            MetadataJson = JsonDocument.Parse("{}"),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
            DeletedAtUtc = null,
            DeletedByUserId = null,
        };
        hazardLayer.EnsureRowVersion();

        _ = _dbContext.HazardLayers.Add(hazardLayer);
        _ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return RegisterHazardLayerResult.Created(
            new HazardLayerDto(
                hazardLayer.Id,
                hazardLayer.PlanId,
                hazardLayer.MapAssetId,
                hazardLayer.Name,
                hazardLayer.HazardType,
                hazardLayer.Severity,
                hazardLayer.Source,
                hazardLayer.Description,
                hazardLayer.IsActive,
                hazardLayer.CreatedAtUtc));
    }
}

public sealed class RegisterHazardLayerResult
{
    private RegisterHazardLayerResult(int statusCode, IReadOnlyList<string>? errors, HazardLayerDto? hazardLayer)
    {
        StatusCode = statusCode;
        Errors = errors;
        HazardLayer = hazardLayer;
    }

    public int StatusCode { get; }

    public IReadOnlyList<string>? Errors { get; }

    public HazardLayerDto? HazardLayer { get; }

    public static RegisterHazardLayerResult Created(HazardLayerDto hazardLayer) => new(201, null, hazardLayer);

    public static RegisterHazardLayerResult ValidationFailed(IReadOnlyList<string> errors) => new(400, errors, null);

    public static RegisterHazardLayerResult Forbidden() => new(403, null, null);

    public static RegisterHazardLayerResult NotFound() => new(404, null, null);

    public static RegisterHazardLayerResult Conflict(IReadOnlyList<string> errors) => new(409, errors, null);
}

