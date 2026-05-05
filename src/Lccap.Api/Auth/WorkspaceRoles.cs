namespace Lccap.Api.Auth;

public static class WorkspaceRoles
{
    public const string SystemAdmin = "SystemAdmin";
    public const string NationalAdmin = "NationalAdmin";
    public const string AgencyAdmin = "AgencyAdmin";
    public const string Admin = "Admin";
    public const string Planner = "Planner";
    public const string Reviewer = "Reviewer";
    public const string Viewer = "Viewer";
    public const string PublicViewer = "PublicViewer";

    public static readonly string[] All =
    [
        SystemAdmin, NationalAdmin, AgencyAdmin, Admin, Planner, Reviewer, Viewer, PublicViewer
    ];
}
