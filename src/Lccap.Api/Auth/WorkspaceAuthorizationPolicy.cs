namespace Lccap.Api.Auth;

public static class WorkspaceAuthorizationPolicy
{
    public static bool CanRead(string? role)
    {
        return NormalizeRole(role) switch
        {
            WorkspaceRoles.Admin => true,
            WorkspaceRoles.Planner => true,
            WorkspaceRoles.Reviewer => true,
            WorkspaceRoles.Viewer => true,
            _ => false
        };
    }

    public static bool CanCreateOrEdit(string? role)
    {
        return NormalizeRole(role) switch
        {
            WorkspaceRoles.Admin => true,
            WorkspaceRoles.Planner => true,
            _ => false
        };
    }

    public static bool CanRestore(string? role)
    {
        return NormalizeRole(role) switch
        {
            WorkspaceRoles.Admin => true,
            WorkspaceRoles.Planner => true,
            _ => false
        };
    }

    public static bool CanExport(string? role)
    {
        return NormalizeRole(role) switch
        {
            WorkspaceRoles.Admin => true,
            WorkspaceRoles.Planner => true,
            WorkspaceRoles.Reviewer => true,
            _ => false
        };
    }

    public static bool CanArchive(string? role)
    {
        return NormalizeRole(role) == WorkspaceRoles.Admin;
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;

        var trimmed = role.Trim();

        foreach (var r in WorkspaceRoles.All)
        {
            if (string.Equals(r, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return r;
            }
        }

        return trimmed;
    }
}
