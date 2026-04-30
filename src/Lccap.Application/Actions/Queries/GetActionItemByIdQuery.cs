using Lccap.Application.Actions.Commands;
using Lccap.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Lccap.Application.Actions.Queries;

public class GetActionItemByIdQuery
{
    private readonly ILccapDbContext _dbContext;
    private readonly ICurrentUserContext _currentUserContext;

    public GetActionItemByIdQuery(ILccapDbContext dbContext, ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _currentUserContext = currentUserContext;
    }

    public async Task<GetActionItemByIdOutcome> ExecuteAsync(
        Guid actionItemId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserContext.AccountId.HasValue)
        {
            return GetActionItemByIdOutcome.ForbiddenOutcome();
        }

        var accountId = _currentUserContext.AccountId.Value;
        var entity = await _dbContext.ActionItems.SingleOrDefaultAsync(
            x => x.Id == actionItemId && x.AccountId == accountId && !x.IsDeleted,
            cancellationToken);

        if (entity is null)
        {
            return GetActionItemByIdOutcome.NotFoundOutcome();
        }

        return GetActionItemByIdOutcome.OkOutcome(new ActionItemDto(entity));
    }
}

public sealed class GetActionItemByIdOutcome
{
    private GetActionItemByIdOutcome(bool isSuccess, bool forbiddenAccess, ActionItemDto? item)
    {
        IsSuccess = isSuccess;
        ForbiddenAccess = forbiddenAccess;
        Item = item;
    }

    public bool IsSuccess { get; }

    public bool ForbiddenAccess { get; }

    public ActionItemDto? Item { get; }

    public static GetActionItemByIdOutcome OkOutcome(ActionItemDto item) => new(true, false, item);

    public static GetActionItemByIdOutcome ForbiddenOutcome() => new(false, true, null);

    public static GetActionItemByIdOutcome NotFoundOutcome() => new(false, false, null);
}
