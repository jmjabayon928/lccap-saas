using Lccap.Application.Common.Interfaces;
using Lccap.Application.HazardLayers.Dtos;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.HazardLayers.Queries;

public sealed class GetPlanHazardLayersQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetPlanHazardLayersQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetPlanHazardLayersResult> Execute(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return GetPlanHazardLayersResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null)
        {
            return GetPlanHazardLayersResult.Forbidden();
        }

        var accountId = _currentUserContext.AccountId.Value;

        var planOk = await _dbContext.Plans.AsNoTracking().AnyAsync(
                p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);

        if (!planOk)
        {
            return GetPlanHazardLayersResult.NotFound();
        }

        var items = await _dbContext.HazardLayers.AsNoTracking()
            .Where(h => h.AccountId == accountId && h.PlanId == planId && !h.IsDeleted)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Select(h => new HazardLayerDto(
                h.Id,
                h.PlanId,
                h.MapAssetId,
                h.Name,
                h.HazardType,
                h.Severity,
                h.Source,
                h.Description,
                h.IsActive,
                h.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return GetPlanHazardLayersResult.Success(items);
    }
}

public sealed class GetPlanHazardLayersResult
{
    private GetPlanHazardLayersResult(int statusCode, IReadOnlyList<HazardLayerDto>? items)
    {
        StatusCode = statusCode;
        Items = items;
    }

    public int StatusCode { get; }

    public IReadOnlyList<HazardLayerDto>? Items { get; }

    public static GetPlanHazardLayersResult Success(IReadOnlyList<HazardLayerDto> items) => new(200, items);

    public static GetPlanHazardLayersResult Unauthorized() => new(401, null);

    public static GetPlanHazardLayersResult Forbidden() => new(403, null);

    public static GetPlanHazardLayersResult NotFound() => new(404, null);
}

