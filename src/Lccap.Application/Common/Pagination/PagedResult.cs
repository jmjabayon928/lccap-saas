using System.Collections.Generic;

namespace Lccap.Application.Common.Pagination;

/// <summary>
/// Standard paged result for list endpoints. Preserves tenant isolation and soft-delete filtering in caller.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);
