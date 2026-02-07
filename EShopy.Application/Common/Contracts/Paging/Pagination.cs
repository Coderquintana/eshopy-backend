namespace EShopy.Application.Common.Contracts.Paging;

public static class Pagination
{
  public const int MaxPageSize = 100;

  public static (IReadOnlyList<T> Items, long Total, int Page, int PageSize) Apply<T>(IReadOnlyList<T> items, int page, int pageSize)
  {
    var normalizedPage = page < 1 ? 1 : page;
    var normalizedPageSize = pageSize switch
    {
      < 1 => 1,
      > MaxPageSize => MaxPageSize,
      _ => pageSize
    };

    var total = items.Count;
    var paged = items
      .Skip((normalizedPage - 1) * normalizedPageSize)
      .Take(normalizedPageSize)
      .ToList();

    return (paged, total, normalizedPage, normalizedPageSize);
  }
}
