namespace Lccap.Application.Common.Pagination;

public sealed record PaginationRequest(int Page = 1, int PageSize = 25)
{
    public (int Page, int PageSize) Normalize() => PaginationHelper.Normalize(Page, PageSize);
}
