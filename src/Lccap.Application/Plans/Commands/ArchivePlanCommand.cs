using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Commands;

public sealed class ArchivePlanCommand
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public ArchivePlanCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<ArchivePlanResult> Execute(Guid planId, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return ArchivePlanResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null || _currentUserContext.UserId is null)
        {
            return ArchivePlanResult.Forbidden();
        }

        var plan = await _dbContext.Plans.SingleOrDefaultAsync(
            p => p.Id == planId && p.AccountId == _currentUserContext.AccountId.Value && !p.IsDeleted,
            cancellationToken);

        if (plan is null)
        {
            return ArchivePlanResult.NotFound();
        }

        var oldValues = new
        {
            plan.Id,
            plan.Title,
            plan.StartYear,
            plan.EndYear,
            plan.Status,
            plan.TemplateMode,
            plan.VersionNumber,
            plan.Description,
            plan.IsDeleted
        };

        var now = DateTimeOffset.UtcNow;
        plan.Archive(now, _currentUserContext.UserId.Value);

        var newValues = new
        {
            Status = "Archived",
            IsDeleted = true,
            DeletedAtUtc = now,
            DeletedByUserId = _currentUserContext.UserId.Value
        };

        var auditLog = new AuditLog
        {
            AccountId = _currentUserContext.AccountId,
            UserId = _currentUserContext.UserId,
            EntityName = "Plan",
            EntityId = plan.Id,
            Action = "PlanArchived",
            OldValuesJson = JsonDocument.Parse(JsonSerializer.Serialize(oldValues)),
            NewValuesJson = JsonDocument.Parse(JsonSerializer.Serialize(newValues)),
            MetadataJson = JsonDocument.Parse(JsonSerializer.Serialize(new { archiveType = "SoftDelete", childRecordsPreserved = true })),
            CreatedAtUtc = now
        };

        _dbContext.AuditLogs.Add(auditLog);

        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return ArchivePlanResult.Success();
    }
}

public sealed class ArchivePlanResult
{
    private ArchivePlanResult(bool isSuccess, int statusCode)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public int StatusCode { get; }

    public static ArchivePlanResult Success() => new(true, 204);
    public static ArchivePlanResult Unauthorized() => new(false, 401);
    public static ArchivePlanResult Forbidden() => new(false, 403);
    public static ArchivePlanResult NotFound() => new(false, 404);
}
