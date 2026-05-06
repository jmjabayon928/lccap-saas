using System.Text.Json;
using System.Text.Json.Serialization;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Maps.Commands;

public sealed class ArchiveMapAssetCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public ArchiveMapAssetCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<ArchiveMapAssetResult> Execute(Guid mapAssetId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return ArchiveMapAssetResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var asset = await _dbContext.MapAssets
            .Where(m => m.Id == mapAssetId && m.AccountId == accountId && !m.IsDeleted)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (asset is null)
        {
            return ArchiveMapAssetResult.NotFound();
        }

        var now = DateTimeOffset.UtcNow;

        asset.IsDeleted = true;
        asset.DeletedAtUtc = now;
        asset.DeletedByUserId = userId;
        asset.UpdatedAtUtc = now;
        asset.UpdatedByUserId = userId;
        asset.RotateRowVersion();

        var features = await _dbContext.GeoJsonLayerFeatures
            .Where(f => f.MapAssetId == mapAssetId && f.AccountId == accountId && !f.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var f in features)
        {
            f.IsDeleted = true;
            f.DeletedAtUtc = now;
            f.DeletedByUserId = userId;
            f.UpdatedAtUtc = now;
            f.UpdatedByUserId = userId;
            f.RotateRowVersion();
        }

        var annotations = await _dbContext.MapAnnotations
            .Where(a => a.MapAssetId == mapAssetId && a.AccountId == accountId && !a.IsDeleted)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var a in annotations)
        {
            a.IsDeleted = true;
            a.DeletedAtUtc = now;
            a.DeletedByUserId = userId;
            a.UpdatedAtUtc = now;
            a.UpdatedByUserId = userId;
            a.RotateRowVersion();
        }

        var newAuditValues = JsonSerializer.SerializeToDocument(
            new { mapAssetId, asset.PlanId },
            AuditJsonOptions);
        var metadata = JsonSerializer.SerializeToDocument(new { planId = asset.PlanId }, AuditJsonOptions);
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "MapAsset",
            EntityId = mapAssetId,
            Action = "MapAssetArchived",
            OldValuesJson = null,
            NewValuesJson = newAuditValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        _ = _dbContext.AuditLogs.Add(audit);

        var planIdForNotify = asset.PlanId;

        _ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await NotificationRecipientResolver.TryPublishWorkspaceEventAsync(
            _dbContext,
            _currentUserContext,
            clock: null,
            "MapAssetArchived",
            "Map layer archived",
            "A map layer was archived.",
            "MapAsset",
            mapAssetId,
            planIdForNotify,
            cancellationToken).ConfigureAwait(false);

        return ArchiveMapAssetResult.NoContent();
    }
}

public sealed class ArchiveMapAssetResult
{
    private ArchiveMapAssetResult(int statusCode)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }

    public static ArchiveMapAssetResult NoContent() => new(204);

    public static ArchiveMapAssetResult Forbidden() => new(403);

    public static ArchiveMapAssetResult NotFound() => new(404);
}
