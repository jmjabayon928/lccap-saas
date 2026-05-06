using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Commands;

public sealed record ReopenSectionCommentRequest(Guid CommentId);

public sealed record ReopenSectionCommentResult(bool Success, bool NotFound, bool ForbiddenAccess, string? Error)
{
    public static ReopenSectionCommentResult Forbidden() => new(false, false, true, null);
    public static ReopenSectionCommentResult Missing() => new(false, true, false, null);
    public static ReopenSectionCommentResult ValidationError(string error) => new(false, false, false, error);
    public static ReopenSectionCommentResult Ok() => new(true, false, false, null);
}

public class ReopenSectionCommentCommand
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public ReopenSectionCommentCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public virtual async Task<ReopenSectionCommentResult> ExecuteAsync(
        ReopenSectionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return ReopenSectionCommentResult.Forbidden();
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;

        var entity = await _db.SectionComments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.AccountId == accountId && !c.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return ReopenSectionCommentResult.Missing();
        }

        if (!entity.IsResolved)
        {
            return ReopenSectionCommentResult.Ok();
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

        entity.IsResolved = false;
        entity.ResolvedAtUtc = null;
        entity.ResolvedByUserId = null;
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
                Action = "SectionCommentReopened",
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
            "SectionCommentReopened",
            "Comment reopened",
            "A review comment was reopened.",
            "SectionComment",
            entity.Id,
            entity.PlanId,
            cancellationToken);

        return ReopenSectionCommentResult.Ok();
    }
}

