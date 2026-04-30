using System.Text.Json;
using Lccap.Application.Common.Interfaces;
using Lccap.Domain.Entities;

namespace Lccap.Application.Plans.Commands;

public sealed class CreatePlanCommand
{
    private static readonly (string SectionKey, string Title, int SortOrder)[] DefaultPlanSections =
    {
        ("executive_summary", "Executive Summary", 10),
        ("introduction", "Introduction and LGU Profile", 20),
        ("climate_risk_assessment", "Climate and Disaster Risk Assessment", 30),
        ("adaptation_actions", "Adaptation Actions", 40),
        ("mitigation_actions", "Mitigation Actions", 50),
        ("implementation_plan", "Implementation Plan", 60),
        ("monitoring_evaluation", "Monitoring and Evaluation", 70),
        ("references_annexes", "References and Annexes", 80),
    };

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

    public CreatePlanCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreatePlanResult> Execute(CreatePlanRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.IsAuthenticated)
        {
            return CreatePlanResult.Unauthorized();
        }

        if (_currentUserContext.AccountId is null || _currentUserContext.UserId is null)
        {
            return CreatePlanResult.Forbidden();
        }

        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return CreatePlanResult.ValidationFailed(validationErrors);
        }

        var now = DateTimeOffset.UtcNow;
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            AccountId = _currentUserContext.AccountId.Value,
            Title = request.Title.Trim(),
            StartYear = request.StartYear,
            EndYear = request.EndYear,
            Status = request.Status,
            TemplateMode = request.TemplateMode,
            VersionNumber = request.VersionNumber,
            Description = request.Description,
            SubmittedAtUtc = request.SubmittedAtUtc,
            ApprovedAtUtc = request.ApprovedAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = _currentUserContext.UserId,
            UpdatedByUserId = _currentUserContext.UserId,
            IsDeleted = false,
        };

        _ = _dbContext.Plans.Add(plan);
        AddDefaultPlanSectionsIfMissing(plan, now);
        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatePlanResult.Success(new PlanDto(plan));
    }

    private void AddDefaultPlanSectionsIfMissing(Plan plan, DateTimeOffset now)
    {
        var accountId = _currentUserContext.AccountId!.Value;
        var createdByUserId = _currentUserContext.UserId;

        foreach (var (sectionKey, title, sortOrder) in DefaultPlanSections)
        {
            var alreadyPresent = _dbContext.PlanSections.Local.Any(
                s => s.PlanId == plan.Id
                    && !s.IsDeleted
                    && string.Equals(s.SectionKey, sectionKey, StringComparison.Ordinal));

            if (alreadyPresent)
            {
                continue;
            }

            var section = new PlanSection
            {
                AccountId = accountId,
                PlanId = plan.Id,
                SectionKey = sectionKey,
                Title = title,
                Content = string.Empty,
                SortOrder = sortOrder,
                SectionMetadataJson = JsonDocument.Parse("{}"),
                CreatedAtUtc = now,
                CreatedByUserId = createdByUserId,
                IsDeleted = false,
            };

            _ = _dbContext.PlanSections.Add(section);
        }
    }

    private static List<string> ValidateRequest(CreatePlanRequest request)
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

        return errors;
    }
}

public sealed record CreatePlanRequest(
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc);

public sealed class CreatePlanResult
{
    private CreatePlanResult(bool isSuccess, int statusCode, PlanDto? plan, IReadOnlyList<string> errors)
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

    public static CreatePlanResult Success(PlanDto plan) => new(true, 201, plan, Array.Empty<string>());

    public static CreatePlanResult ValidationFailed(IReadOnlyList<string> errors) => new(false, 400, null, errors);

    public static CreatePlanResult Unauthorized() => new(false, 401, null, Array.Empty<string>());

    public static CreatePlanResult Forbidden() => new(false, 403, null, Array.Empty<string>());
}

public sealed record PlanDto(
    Guid Id,
    Guid AccountId,
    string Title,
    int StartYear,
    int EndYear,
    string Status,
    string TemplateMode,
    int VersionNumber,
    string? Description,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ApprovedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    Guid? CreatedByUserId,
    Guid? UpdatedByUserId,
    byte[] RowVersion)
{
    public PlanDto(Plan plan)
        : this(
            plan.Id,
            plan.AccountId,
            plan.Title,
            plan.StartYear,
            plan.EndYear,
            plan.Status,
            plan.TemplateMode,
            plan.VersionNumber,
            plan.Description,
            plan.SubmittedAtUtc,
            plan.ApprovedAtUtc,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.CreatedByUserId,
            plan.UpdatedByUserId,
            plan.RowVersion)
    {
    }
}
