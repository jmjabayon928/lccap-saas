using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Commands;

public sealed record ResolveSectionCommentRequest(Guid CommentId);

public sealed record ResolveSectionCommentResult(bool Success, bool NotFound, bool ForbiddenAccess, string? Error)
{
    public static ResolveSectionCommentResult Forbidden() => new(false, false, true, null);
    public static ResolveSectionCommentResult Missing() => new(false, true, false, null);
    public static ResolveSectionCommentResult ValidationError(string error) => new(false, false, false, error);
    public static ResolveSectionCommentResult Ok() => new(true, false, false, null);
}

public class ResolveSectionCommentCommand
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public ResolveSectionCommentCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public virtual async Task<ResolveSectionCommentResult> ExecuteAsync(
        ResolveSectionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return ResolveSectionCommentResult.Forbidden();
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;

        var entity = await _db.SectionComments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.AccountId == accountId && !c.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return ResolveSectionCommentResult.Missing();
        }

        if (entity.IsResolved)
        {
            return ResolveSectionCommentResult.Ok();
        }

        var now = _clock.UtcNow;
        var oldValues = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    isResolved = entity.IsResolved,
                    resolvedAtUtc = entity.ResolvedAtUtc,
                    resolvedByUserId = entity.ResolvedByUserId
                }));

        entity.IsResolved = true;
        entity.ResolvedAtUtc = now;
        entity.ResolvedByUserId = userId;
        entity.UpdatedAtUtc = now;
        entity.UpdatedByUserId = userId;
        entity.RotateRowVersion();

        var newValues = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    isResolved = entity.IsResolved,
                    resolvedAtUtc = entity.ResolvedAtUtc,
                    resolvedByUserId = entity.ResolvedByUserId
                }));

        var metadata = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    planId = entity.PlanId,
                    sectionKey = entity.SectionKey,
                    commentId = entity.Id,
                    commentType = entity.CommentType
                }));

        _db.AuditLogs.Add(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                UserId = userId,
                Action = "SectionCommentResolved",
                EntityName = "SectionComment",
                EntityId = entity.Id,
                OldValuesJson = oldValues,
                NewValuesJson = newValues,
                MetadataJson = metadata,
                CreatedAtUtc = now
            });

        _ = await _db.SaveChangesAsync(cancellationToken);

        await NotificationRecipientResolver.TryPublishWorkspaceEventAsync(
            _db,
            _currentUser,
            _clock,
            "SectionCommentResolved",
            "Comment resolved",
            "A review comment was marked resolved.",
            "SectionComment",
            entity.Id,
            entity.PlanId,
            cancellationToken);

        return ResolveSectionCommentResult.Ok();
    }
}

