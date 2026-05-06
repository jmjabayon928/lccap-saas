using Lccap.Api.Auth;
using Lccap.Application.Notifications.Commands;
using Lccap.Application.Notifications.Queries;
using Lccap.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lccap.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class NotificationsController : ControllerBase
{
    private readonly ICurrentUserContext _currentUser;

    public NotificationsController(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("notifications")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int? limit,
        [FromQuery] bool? unreadOnly,
        [FromServices] GetMyNotificationsQuery query,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return Forbid();
        }

        var result = await query.Execute(
            new GetMyNotificationsRequest(limit ?? 25, unreadOnly ?? false),
            cancellationToken);

        return result.StatusCode switch
        {
            200 => Ok(new
            {
                items = result.Items,
                unreadCount = result.UnreadCount,
                totalCount = result.TotalCount,
                limit = result.Limit,
                unreadOnly = result.UnreadOnly
            }),
            403 => Forbid(),
            400 => BadRequest(new { errors = result.Errors }),
            _ => StatusCode(result.StatusCode)
        };
    }

    [HttpPost("notifications/{notificationId:guid}/read")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> MarkNotificationRead(
        Guid notificationId,
        [FromServices] MarkNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return Forbid();
        }

        var result = await command.Execute(new MarkNotificationReadRequest(notificationId), cancellationToken);

        return result.StatusCode switch
        {
            200 => NoContent(),
            404 => NotFound(),
            403 => Forbid(),
            _ => StatusCode(result.StatusCode)
        };
    }

    [HttpPost("notifications/read-all")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> MarkAllNotificationsRead(
        [FromServices] MarkAllNotificationsReadCommand command,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null || _currentUser.UserId is null)
        {
            return Forbid();
        }

        var result = await command.Execute(cancellationToken);

        return result.StatusCode switch
        {
            200 => Ok(new { updatedCount = result.UpdatedCount }),
            403 => Forbid(),
            _ => StatusCode(result.StatusCode)
        };
    }

    [HttpGet("collaboration/summary")]
    [RequireWorkspaceRole("Read")]
    public async Task<IActionResult> GetCollaborationSummary(
        [FromServices] GetCollaborationSummaryQuery query,
        CancellationToken cancellationToken)
    {
        if (!WorkspaceAuthorizationPolicy.CanRead(_currentUser.Role))
        {
            return Forbid();
        }

        if (_currentUser.AccountId is null)
        {
            return Forbid();
        }

        var result = await query.Execute(cancellationToken);

        return result.StatusCode switch
        {
            200 => Ok(new
            {
                groups = result.Groups,
                totalGroups = result.TotalGroups,
                totalMembers = result.TotalMembers
            }),
            403 => Forbid(),
            400 => BadRequest(new { errors = result.Errors }),
            _ => StatusCode(result.StatusCode)
        };
    }
}

