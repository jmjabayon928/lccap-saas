using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Lccap.Application.Audit.Queries;

public sealed class GetAuditLogsQuery
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public GetAuditLogsQuery(ILccapDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GetAuditLogsResult> Execute(GetAuditLogsRequest request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            return GetAuditLogsResult.Unauthorized();
        }

        if (!_currentUser.AccountId.HasValue)
        {
            return GetAuditLogsResult.Forbidden();
        }

        var query = _db.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Where(a => a.AccountId == _currentUser.AccountId.Value);

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            query = query.Where(a => a.EntityName == request.EntityName);
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            query = query.Where(a => a.Action == request.Action);
        }

        if (request.UserId.HasValue)
        {
            query = query.Where(a => a.UserId == request.UserId.Value);
        }

        if (request.FromUtc.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc >= request.FromUtc.Value);
        }

        if (request.ToUtc.HasValue)
        {
            query = query.Where(a => a.CreatedAtUtc <= request.ToUtc.Value);
        }

        // Fetch logs to apply JSON filtering and pagination.
        var logs = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (request.PlanId.HasValue)
        {
            var planIdStr = request.PlanId.Value.ToString();
            logs = logs.Where(a => 
                a.MetadataJson.RootElement.TryGetProperty("planId", out var prop) && 
                prop.ValueKind == JsonValueKind.String &&
                prop.GetString() == planIdStr).ToList();
        }

        var totalCount = logs.Count;
        var pageSize = Math.Min(request.PageSize ?? 25, 100);
        var page = Math.Max(request.Page ?? 1, 1);
        
        var items = logs
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogListItemDto(
                a.Id,
                a.AccountId,
                a.UserId,
                a.User?.Email,
                a.User?.FullName,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.OldValuesJson,
                a.NewValuesJson,
                a.MetadataJson,
                a.IpAddress,
                a.CreatedAtUtc))
            .ToList();

        return GetAuditLogsResult.Success(new AuditLogPagedResultDto(items, page, pageSize, totalCount));
    }
}

public sealed record GetAuditLogsRequest(
    string? EntityName = null,
    string? Action = null,
    Guid? UserId = null,
    Guid? PlanId = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    int? Page = 1,
    int? PageSize = 25);

public sealed class GetAuditLogsResult
{
    private GetAuditLogsResult(bool isSuccess, int statusCode, AuditLogPagedResultDto? result)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Result = result;
    }

    public bool IsSuccess { get; }
    public int StatusCode { get; }
    public AuditLogPagedResultDto? Result { get; }

    public static GetAuditLogsResult Success(AuditLogPagedResultDto result) => new(true, 200, result);
    public static GetAuditLogsResult Unauthorized() => new(false, 401, null);
    public static GetAuditLogsResult Forbidden() => new(false, 403, null);
}

public sealed record AuditLogPagedResultDto(
    List<AuditLogListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record AuditLogListItemDto(
    Guid Id,
    Guid? AccountId,
    Guid? UserId,
    string? UserEmail,
    string? UserFullName,
    string EntityName,
    Guid? EntityId,
    string Action,
    JsonDocument? OldValuesJson,
    JsonDocument? NewValuesJson,
    JsonDocument MetadataJson,
    string? IpAddress,
    DateTimeOffset CreatedAtUtc);
