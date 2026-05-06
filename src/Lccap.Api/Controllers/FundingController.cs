using Lccap.Api.Auth;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Funding.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class FundingController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public FundingController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("funding/climate-expenditure-tags")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetClimateExpenditureTags(
        [FromServices] GetClimateExpenditureTagsQuery query,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        var result = await query.ExecuteAsync(includeInactive, cancellationToken);
        return Ok(result);
    }
}
