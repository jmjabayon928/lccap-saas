using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Plans.Commands;

public sealed class UpdatePlanCommand
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.Ordinal)
    {
        "Draft",
        "InProgress",
        "ReadyForExport",
        "Submitted",
        "Approved",
        "Archived",
    };

    private static readonly HashSet<string> AllowedTemplateModes = new(StringComparer.Ordinal)
    {
        "New",
        "Partial",
        "Enhancement",
    };

    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public UpdatePlanCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<UpdatePlanResult> Execute(Guid planId, UpdatePlanRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return UpdatePlanResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null || _currentUserContext.UserId is null)
        {
            return UpdatePlanResult.Forbidden();
        }

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return UpdatePlanResult.ValidationFailed(validationErrors);
        }

        var plan = await _dbContext.Plans.SingleOrDefaultAsync(
            p => p.Id == planId && p.AccountId == _currentUserContext.AccountId.Value && !p.IsDeleted,
            cancellationToken);

        if (plan is null)
        {
            return UpdatePlanResult.NotFound();
        }

        if (!plan.RowVersion.AsSpan().SequenceEqual(request.RowVersion))
        {
            return UpdatePlanResult.ConcurrencyConflict();
        }

        plan.Title = request.Title.Trim();
        plan.StartYear = request.StartYear;
        plan.EndYear = request.EndYear;
        plan.Status = request.Status;
        plan.TemplateMode = request.TemplateMode;
        plan.VersionNumber = request.VersionNumber;
        plan.Description = request.Description;
        plan.SubmittedAtUtc = request.SubmittedAtUtc;
        plan.ApprovedAtUtc = request.ApprovedAtUtc;
        plan.UpdatedAtUtc = DateTimeOffset.UtcNow;
        plan.UpdatedByUserId = _currentUserContext.UserId;

        try
        {
            _ = await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return UpdatePlanResult.ConcurrencyConflict();
        }

        return UpdatePlanResult.Success(new PlanDto(plan));
    }

    private static List<string> ValidateRequest(UpdatePlanRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            errors.Add("Title must not be blank.");
        }

        if (request.StartYear < 2000 || request.StartYear > 2100 || request.EndYear < 2000 || request.EndYear > 2100)
        {
            errors.Add("StartYear and EndYear must be between 2000 and 2100.");
        }

        if (request.StartYear > request.EndYear)
        {
            errors.Add("StartYear must be less than or equal to EndYear.");
        }

        if (!AllowedStatuses.Contains(request.Status))
        {
            errors.Add("Status is invalid.");
        }

        if (!AllowedTemplateModes.Contains(request.TemplateMode))
        {
            errors.Add("TemplateMode is invalid.");
        }

        if (request.VersionNumber < 1)
        {
            errors.Add("VersionNumber must be greater than zero.");
        }

        if (request.RowVersion is null || request.RowVersion.Length == 0)
        {
            errors.Add("RowVersion is required.");
        }

        return errors;
    }
}

public sealed record UpdatePlanRequest(
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    byte[] RowVersion);

public sealed class UpdatePlanResult
{
    private UpdatePlanResult(bool isSuccess, int statusCode, PlanDto? plan, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Plan = plan;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public PlanDto? Plan { get; }

    public IReadOnlyList<string> Errors { get; }

    public static UpdatePlanResult Success(PlanDto plan) => new(true, 200, plan, Array.Empty<string>());

    public static UpdatePlanResult ValidationFailed(IReadOnlyList<string> errors) => new(false, 400, null, errors);

    public static UpdatePlanResult Unauthorized() => new(false, 401, null, Array.Empty<string>());

    public static UpdatePlanResult Forbidden() => new(false, 403, null, Array.Empty<string>());

    public static UpdatePlanResult NotFound() => new(false, 404, null, Array.Empty<string>());

    public static UpdatePlanResult ConcurrencyConflict() => new(false, 409, null, Array.Empty<string>());
}
