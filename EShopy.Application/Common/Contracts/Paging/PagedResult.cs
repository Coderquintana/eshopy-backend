namespace EShopy.Application.Common.Contracts.Paging;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
  public long TotalPages => PageSize > 0 ? (long)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}
