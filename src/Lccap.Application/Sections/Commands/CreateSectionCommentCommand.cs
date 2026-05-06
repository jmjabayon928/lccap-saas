using Lccap.Application.Common.Interfaces;
using Lccap.Application.Notifications;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Lccap.Application.Sections.Commands;

public sealed record CreateSectionCommentRequest(Guid PlanId, string SectionKey, string CommentType, string CommentText);

public sealed record SectionCommentDto(
    Guid Id,
    Guid PlanId,
    string SectionKey,
    string CommentType,
    string CommentText,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    bool IsResolved,
    DateTimeOffset? ResolvedAtUtc,
    Guid? ResolvedByUserId,
    DateTimeOffset? UpdatedAtUtc,
    byte[] RowVersion);

public sealed record CreateSectionCommentResult(
    bool Success,
    bool NotFound,
    bool ForbiddenAccess,
    string? Error,
    SectionCommentDto? Comment)
{
    public static CreateSectionCommentResult Forbidden() => new(false, false, true, null, null);
    public static CreateSectionCommentResult Missing() => new(false, true, false, null, null);
    public static CreateSectionCommentResult ValidationError(string error) => new(false, false, false, error, null);
    public static CreateSectionCommentResult Ok(SectionCommentDto comment) => new(true, false, false, null, comment);
}

public class CreateSectionCommentCommand
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
    {
        "General",
        "DataGap",
        "Validation",
        "RevisionRequest"
    };

    private readonly ILccapDbContext _db;
    private readonly ICurrentUserContext _currentUser;
    private readonly IClock _clock;

    public CreateSectionCommentCommand(ILccapDbContext db, ICurrentUserContext currentUser, IClock clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public virtual async Task<CreateSectionCommentResult> ExecuteAsync(
        CreateSectionCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return CreateSectionCommentResult.Forbidden();
        }

        var sectionKey = (request.SectionKey ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sectionKey))
        {
            return CreateSectionCommentResult.ValidationError("Section key is required.");
        }
        if (sectionKey.Length > 100)
        {
            return CreateSectionCommentResult.ValidationError("Section key is too long.");
        }

        var commentTypeRaw = (request.CommentType ?? string.Empty).Trim();
        if (!AllowedTypes.Contains(commentTypeRaw))
        {
            return CreateSectionCommentResult.ValidationError("Invalid comment type.");
        }

        var commentText = (request.CommentText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(commentText))
        {
            return CreateSectionCommentResult.ValidationError("Comment text is required.");
        }
        if (commentText.Length > 4000)
        {
            return CreateSectionCommentResult.ValidationError("Comment text is too long.");
        }

        var accountId = _currentUser.AccountId.Value;
        var userId = _currentUser.UserId.Value;

        var planExists = await _db.Plans
            .AsNoTracking()
            .AnyAsync(p => p.Id == request.PlanId && p.AccountId == accountId && !p.IsDeleted, cancellationToken);
        if (!planExists)
        {
            return CreateSectionCommentResult.Missing();
        }

        var sectionExists = await _db.PlanSections
            .AsNoTracking()
            .AnyAsync(
                s => s.PlanId == request.PlanId
                    && s.AccountId == accountId
                    && !s.IsDeleted
                    && s.SectionKey == sectionKey,
                cancellationToken);
        if (!sectionExists)
        {
            return CreateSectionCommentResult.Missing();
        }

        var now = _clock.UtcNow;
        var commentId = Guid.NewGuid();
        var entity = new SectionComment
        {
            Id = commentId,
            AccountId = accountId,
            PlanId = request.PlanId,
            SectionKey = sectionKey,
            CommentType = commentTypeRaw,
            CommentText = commentText,
            CreatedByUserId = userId,
            IsResolved = false,
            CreatedAtUtc = now,
            IsDeleted = false,
        };

        entity.EnsureRowVersion();
        _db.SectionComments.Add(entity);

        var metadata = JsonDocument.Parse(
            JsonSerializer.Serialize(
                new
                {
                    planId = request.PlanId,
                    sectionKey,
                    commentId,
                    commentType = commentTypeRaw
                }));

        _db.AuditLogs.Add(
            new AuditLog
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                UserId = userId,
                Action = "SectionCommentCreated",
                EntityName = "SectionComment",
                EntityId = commentId,
                MetadataJson = metadata,
                CreatedAtUtc = now
            });

        _ = await _db.SaveChangesAsync(cancellationToken);

        await NotificationRecipientResolver.TryPublishWorkspaceEventAsync(
            _db,
            _currentUser,
            _clock,
            "SectionCommentCreated",
            "Review comment added",
            $"A {commentTypeRaw} comment was added for this plan.",
            "SectionComment",
            commentId,
            request.PlanId,
            cancellationToken);

        return CreateSectionCommentResult.Ok(
            new SectionCommentDto(
                entity.Id,
                entity.PlanId,
                entity.SectionKey,
                entity.CommentType,
                entity.CommentText,
                entity.CreatedByUserId,
                entity.CreatedAtUtc,
                entity.IsResolved,
                entity.ResolvedAtUtc,
                entity.ResolvedByUserId,
                entity.UpdatedAtUtc,
                entity.RowVersion));
    }
}

