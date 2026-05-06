using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Sections.Commands;

public sealed record ArchiveSectionCommentRequest(Guid CommentId);

public sealed record ArchiveSectionCommentResult(bool Success, bool NotFound, bool ForbiddenAccess, string? Error)
{
    public static ArchiveSectionCommentResult Forbidden() => new(false, false, true, null);
    public static ArchiveSectionCommentResult Missing() => new(false, true, false, null);
    public static ArchiveSectionCommentResult ValidationError(string error) => new(false, false, false, error);
    public static ArchiveSectionCommentResult Ok() => new(true, false, false, null);
}

public class ArchiveSectionCommentCommand
{
    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public ArchiveSectionCommentCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public virtual async Task<ArchiveSectionCommentResult> ExecuteAsync(
        ArchiveSectionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return ArchiveSectionCommentResult.Forbidden();
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;

        var entity = await _db.SectionComments
            .FirstOrDefaultAsync(c => c.Id == request.CommentId && c.AccountId == accountId && !c.IsDeleted, cancellationToken);
        if (entity is null)
        {
            return ArchiveSectionCommentResult.Missing();
        }

        var now = _clock.UtcNow;
        var oldValues = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    isDeleted = entity.IsDeleted,
                    deletedAtUtc = entity.DeletedAtUtc,
                    deletedByUserId = entity.DeletedByUserId
                }));

        entity.IsDeleted = true;
        entity.DeletedAtUtc = now;
        entity.DeletedByUserId = userId;
        entity.UpdatedAtUtc = now;
        entity.UpdatedByUserId = userId;
        entity.RotateRowVersion();

        var newValues = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    isDeleted = entity.IsDeleted,
                    deletedAtUtc = entity.DeletedAtUtc,
                    deletedByUserId = entity.DeletedByUserId
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
                Action = "SectionCommentArchived",
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
            "SectionCommentArchived",
            "Comment archived",
            "A review comment was archived.",
            "SectionComment",
            entity.Id,
            entity.PlanId,
            cancellationToken);

        return ArchiveSectionCommentResult.Ok();
    }
}

