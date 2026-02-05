namespace EShopy.Application.Common.Contracts.Paging;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
