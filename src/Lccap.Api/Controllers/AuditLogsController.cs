using Lccap.Api.Auth;
using Lccap.Application.Audit.Queries;
using Lccap.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public AuditLogsController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] string? entityName,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? planId,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromServices] GetAuditLogsQuery query = null!,
        CancellationToken cancellationToken = default)
    {
        // RBAC: Admin and Reviewer only for Audit Logs.
        var role = _currentUser.Role;
        var isAdmin = string.Equals(role, WorkspaceRoles.Admin, StringComparison.OrdinalIgnoreCase);
        var isReviewer = string.Equals(role, WorkspaceRoles.Reviewer, StringComparison.OrdinalIgnoreCase);

        if (!isAdmin && !isReviewer)
        {
            return Forbid();
        }

        var result = await query.Execute(
            new GetAuditLogsRequest(entityName, action, userId, planId, fromUtc, toUtc, page, pageSize),
            cancellationToken);

        return result.StatusCode switch
        {
            StatusCodes.Status200OK => Ok(result.Result),
            StatusCodes.Status401Unauthorized => Unauthorized(),
            StatusCodes.Status403Forbidden => Forbid(),
            _ => StatusCode(result.StatusCode),
        };
    }
}
