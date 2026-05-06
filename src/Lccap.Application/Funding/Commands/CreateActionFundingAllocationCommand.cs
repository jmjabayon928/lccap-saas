using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Funding.Queries;
using Lccap.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Funding.Commands;

public sealed class CreateActionFundingAllocationCommand
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly Regex CurrencyCodeRegex = new("^[A-Z]{3}$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly HashSet<string> AllowedAllocationStatuses = new(StringComparer.Ordinal)
    {
        "Planned",
        "Committed",
        "PartiallyReleased",
        "Released",
        "PartiallySpent",
        "Spent",
        "Cancelled",
    };

    private readonly ILccapDbContext _dbContext;

    private readonly ICurrentUserContext _currentUserContext;

    public CreateActionFundingAllocationCommand(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<CreateActionFundingAllocationOutcome> ExecuteAsync(
        Guid planId,
        CreateActionFundingAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_currentUserContext.AccountId.HasValue || !_currentUserContext.UserId.HasValue)
        {
            return CreateActionFundingAllocationOutcome.ForbiddenAccount();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var userId = _currentUserContext.UserId.Value;

        var errors = ValidateRequest(request);
        if (errors.Count > 0)
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(errors);
        }

        var notes = NormalizeNotes(request.Notes);
        var currency = NormalizeCurrency(request.CurrencyCode);
        if (!CurrencyCodeRegex.IsMatch(currency))
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(
                new[] { "Currency code must be exactly three uppercase letters." });
        }

        var (allocationStatusOrNull, allocationError) = ResolveAllocationStatusForCreate(request.AllocationStatus);
        if (allocationStatusOrNull is null)
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(new[] { allocationError ?? "Invalid allocation status." });
        }

        var allocationStatus = allocationStatusOrNull;

        var planOk = await _dbContext.Plans.AnyAsync(
            p => p.Id == planId && p.AccountId == accountId && !p.IsDeleted,
            cancellationToken);
        if (!planOk)
        {
            return CreateActionFundingAllocationOutcome.PlanNotFound();
        }

        var actionItem = await _dbContext.ActionItems.FirstOrDefaultAsync(
            a => a.Id == request.ActionItemId && a.AccountId == accountId && !a.IsDeleted,
            cancellationToken);
        if (actionItem is null)
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(new[] { "Action item was not found." });
        }

        if (actionItem.PlanId != planId)
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(
                new[] { "Action item does not belong to this plan." });
        }

        var fundingSource = await _dbContext.FundingSources.FirstOrDefaultAsync(
            s => s.Id == request.FundingSourceId && s.AccountId == accountId && !s.IsDeleted,
            cancellationToken);
        if (fundingSource is null)
        {
            return CreateActionFundingAllocationOutcome.ValidationFailure(new[] { "Funding source was not found." });
        }

        if (request.FundingProgramId.HasValue)
        {
            var program = await _dbContext.FundingPrograms.FirstOrDefaultAsync(
                p => p.Id == request.FundingProgramId.Value && p.AccountId == accountId && !p.IsDeleted,
                cancellationToken);
            if (program is null)
            {
                return CreateActionFundingAllocationOutcome.ValidationFailure(new[] { "Funding program was not found." });
            }

            if (program.FundingSourceId != request.FundingSourceId)
            {
                return CreateActionFundingAllocationOutcome.ValidationFailure(
                    new[] { "Funding program does not belong to the selected funding source." });
            }
        }

        if (request.ClimateExpenditureTagId.HasValue)
        {
            var tag = await _dbContext.ClimateExpenditureTags.FirstOrDefaultAsync(
                t => t.Id == request.ClimateExpenditureTagId.Value && t.AccountId == accountId && !t.IsDeleted,
                cancellationToken);
            if (tag is null)
            {
                return CreateActionFundingAllocationOutcome.ValidationFailure(new[] { "Climate expenditure tag was not found." });
            }

            if (!tag.IsActive)
            {
                return CreateActionFundingAllocationOutcome.ValidationFailure(
                    new[] { "Climate expenditure tag must be active." });
            }
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new ActionFundingAllocation
        {
            AccountId = accountId,
            PlanId = planId,
            ActionItemId = request.ActionItemId,
            FundingSourceId = request.FundingSourceId,
            FundingProgramId = request.FundingProgramId,
            FundingApplicationId = null,
            ClimateExpenditureTagId = request.ClimateExpenditureTagId,
            FiscalYear = request.FiscalYear,
            AllocatedAmount = request.AllocatedAmount,
            CommittedAmount = null,
            ReleasedAmount = null,
            SpentAmount = null,
            CurrencyCode = currency,
            AllocationStatus = allocationStatus,
            Notes = notes,
            MetadataJson = JsonDocument.Parse("{}"),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
            IsDeleted = false,
        };
        entity.EnsureRowVersion();

        _ = _dbContext.ActionFundingAllocations.Add(entity);

        var newAuditValues = JsonSerializer.SerializeToDocument(
            new
            {
                entity.Id,
                entity.PlanId,
                entity.ActionItemId,
                entity.FundingSourceId,
                entity.FundingProgramId,
                entity.ClimateExpenditureTagId,
                entity.FiscalYear,
                entity.AllocatedAmount,
                entity.CurrencyCode,
                entity.AllocationStatus,
            },
            AuditJsonOptions);

        var metadata = JsonSerializer.SerializeToDocument(
            new { planId, actionItemId = request.ActionItemId },
            AuditJsonOptions);

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            UserId = userId,
            EntityName = "ActionFundingAllocation",
            EntityId = entity.Id,
            Action = "ActionFundingAllocationCreated",
            OldValuesJson = null,
            NewValuesJson = newAuditValues,
            MetadataJson = metadata,
            CreatedAtUtc = now,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
        };
        _ = _dbContext.AuditLogs.Add(audit);

        _ = await _dbContext.SaveChangesAsync(cancellationToken);

        FundingProgram? programNav = null;
        if (request.FundingProgramId.HasValue)
        {
            programNav = await _dbContext.FundingPrograms.AsNoTracking().FirstAsync(
                p => p.Id == request.FundingProgramId.Value,
                cancellationToken);
        }

        ClimateExpenditureTag? tagNav = null;
        if (request.ClimateExpenditureTagId.HasValue)
        {
            tagNav = await _dbContext.ClimateExpenditureTags.AsNoTracking().FirstAsync(
                t => t.Id == request.ClimateExpenditureTagId.Value,
                cancellationToken);
        }

        var dto = new ActionFundingAllocationListItemDto(
            entity.Id,
            entity.PlanId,
            entity.ActionItemId,
            actionItem.Title,
            entity.FundingSourceId,
            fundingSource.Name,
            entity.FundingProgramId,
            programNav?.Name,
            entity.ClimateExpenditureTagId,
            tagNav?.TagCode,
            tagNav?.TagName,
            tagNav?.TagCategory,
            entity.FiscalYear,
            entity.AllocatedAmount,
            entity.CurrencyCode,
            entity.AllocationStatus,
            entity.Notes,
            entity.CreatedAtUtc);

        return CreateActionFundingAllocationOutcome.Created(dto);
    }

    private static List<string> ValidateRequest(CreateActionFundingAllocationRequest request)
    {
        var errors = new List<string>();

        if (request.FiscalYear is < 2000 or > 2100)
        {
            errors.Add("Fiscal year must be between 2000 and 2100 inclusive.");
        }

        if (request.AllocatedAmount < 0)
        {
            errors.Add("Allocated amount cannot be negative.");
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return errors;
    }

    private static string NormalizeCurrency(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "PHP";
        }

        return raw.Trim().ToUpperInvariant();
    }

    /// <summary>Foundation slice: only Planned on create; unknown values return error text.</summary>
    private static (string? Status, string? Error) ResolveAllocationStatusForCreate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return ("Planned", null);
        }

        var s = raw.Trim();
        if (!AllowedAllocationStatuses.Contains(s))
        {
            return (null, "Allocation status is invalid.");
        }

        return s.Equals("Planned", StringComparison.Ordinal)
            ? ("Planned", null)
            : (null, "Only Planned status is allowed when creating allocations in this slice.");
    }

    private static string? NormalizeNotes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return raw.Trim();
    }
}

public sealed record CreateActionFundingAllocationRequest(
    Guid ActionItemId,
    Guid FundingSourceId,
    Guid? FundingProgramId,
    Guid? ClimateExpenditureTagId,
    int FiscalYear,
    decimal AllocatedAmount,
    string? CurrencyCode,
    string? AllocationStatus,
    string? Notes);

public sealed class CreateActionFundingAllocationOutcome
{
    private CreateActionFundingAllocationOutcome(
        bool isSuccess,
        int statusCode,
        ActionFundingAllocationListItemDto? dto,
        bool forbiddenAccess,
        bool missingPlan,
        IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Dto = dto;
        ForbiddenAccess = forbiddenAccess;
        MissingPlan = missingPlan;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public int StatusCode { get; }

    public ActionFundingAllocationListItemDto? Dto { get; }

    public bool ForbiddenAccess { get; }

    public bool MissingPlan { get; }

    public IReadOnlyList<string> Errors { get; }

    public static CreateActionFundingAllocationOutcome Created(ActionFundingAllocationListItemDto dto) =>
        new(true, 201, dto, false, false, Array.Empty<string>());

    public static CreateActionFundingAllocationOutcome ValidationFailure(IReadOnlyList<string> errors) =>
        new(false, 400, null, false, false, errors);

    public static CreateActionFundingAllocationOutcome ForbiddenAccount() =>
        new(false, 403, null, true, false, Array.Empty<string>());

    public static CreateActionFundingAllocationOutcome PlanNotFound() =>
        new(false, 404, null, false, true, Array.Empty<string>());
}
