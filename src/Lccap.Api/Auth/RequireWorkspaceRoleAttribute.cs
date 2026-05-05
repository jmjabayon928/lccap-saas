using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Lccap.Application.Common.Interfaces;

namespace Lccap.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireWorkspaceRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly Func<string?, bool> _policyCheck;

    public RequireWorkspaceRoleAttribute(string policyName)
    {
        _policyCheck = policyName.ToLowerInvariant() switch
        {
            "read" => WorkspaceAuthorizationPolicy.CanRead,
            "createoredit" => WorkspaceAuthorizationPolicy.CanCreateOrEdit,
            "restore" => WorkspaceAuthorizationPolicy.CanRestore,
            "export" => WorkspaceAuthorizationPolicy.CanExport,
            "archive" => WorkspaceAuthorizationPolicy.CanArchive,
            _ => throw new ArgumentException($"Unknown policy: {policyName}")
        };
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userContext = context.HttpContext.RequestServices.GetRequiredService<ICurrentUserContext>();

        if (!userContext.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!_policyCheck(userContext.Role))
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
