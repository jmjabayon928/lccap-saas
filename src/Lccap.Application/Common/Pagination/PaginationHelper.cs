namespace Lccap.Application.Common.Pagination;

/// <summary>
/// Helper for normalizing and clamping pagination parameters.
/// </summary>
public static class PaginationHelper
{
    public static (int Page, int PageSize) Normalize(int? page, int? pageSize)
    {
        var p = page.HasValue && page.Value >= 1 ? page.Value : 1;
        var ps = pageSize.HasValue ? Math.Clamp(pageSize.Value, 1, 100) : 25;
        return (p, ps);
    }
}
