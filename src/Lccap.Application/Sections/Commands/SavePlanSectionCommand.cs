using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;

namespace Lccap.Application.Sections.Commands;

public class SavePlanSectionCommand
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public SavePlanSectionCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public virtual async Task<SavePlanSectionResult> ExecuteAsync(
        SavePlanSectionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue)
        {
            return SavePlanSectionResult.Forbidden();
        }

        if (!_currentUserContext.UserId.HasValue)
        {
            return SavePlanSectionResult.Forbidden();
        }

        if (string.IsNullOrWhiteSpace(request.SectionKey))
        {
            return SavePlanSectionResult.ValidationError("Section key is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return SavePlanSectionResult.ValidationError("Title is required.");
        }

        if (request.SortOrder < 0)
        {
            return SavePlanSectionResult.ValidationError("Sort order cannot be negative.");
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var planExists = _dbContext.Plans.Any(p => p.Id == request.PlanId && p.AccountId == accountId && !p.IsDeleted);
        if (!planExists)
        {
            return SavePlanSectionResult.Missing();
        }

        var normalizedKey = request.SectionKey.Trim();
        var section = _dbContext.PlanSections.SingleOrDefault(
            x => x.AccountId == accountId
                && x.PlanId == request.PlanId
                && x.SectionKey == normalizedKey
                && !x.IsDeleted);

        var now = DateTimeOffset.UtcNow;
        if (section is null)
        {
            section = new PlanSection
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                PlanId = request.PlanId,
                SectionKey = normalizedKey,
                Title = request.Title.Trim(),
                Content = request.Content ?? string.Empty,
                SortOrder = request.SortOrder,
                LastEditedByUserId = userId,
                LastEditedAtUtc = now,
                SectionMetadataJson = JsonDocument.Parse("{}"),
                CreatedAtUtc = now,
                CreatedByUserId = userId,
                IsDeleted = false,
            };
            _ = _dbContext.PlanSections.Add(section);
        }
        else
        {
            section.SortOrder = request.SortOrder;
            section.UpdateContent(request.Title, request.Content ?? string.Empty, userId, now);
        }

        _ = await _dbContext.SaveChangesAsync(cancellationToken);
        return SavePlanSectionResult.Ok(section.Id, section.LastEditedByUserId, section.LastEditedAtUtc);
    }
}

public sealed record SavePlanSectionRequest(Guid PlanId, string SectionKey, string Title, string? Content, int SortOrder);

public sealed record SavePlanSectionResult(
    bool Success,
    bool ForbiddenAccess,
    bool NotFound,
    Guid? PlanSectionId,
    Guid? LastEditedByUserId,
    DateTimeOffset? LastEditedAtUtc,
    string? Error)
{
    public static SavePlanSectionResult Ok(Guid sectionId, Guid? lastEditedByUserId, DateTimeOffset? lastEditedAtUtc) =>
        new(true, false, false, sectionId, lastEditedByUserId, lastEditedAtUtc, null);

    public static SavePlanSectionResult ValidationError(string error) =>
        new(false, false, false, null, null, null, error);

    public static SavePlanSectionResult Missing() =>
        new(false, false, true, null, null, null, null);

    public static SavePlanSectionResult Forbidden() =>
        new(false, true, false, null, null, null, null);
}
