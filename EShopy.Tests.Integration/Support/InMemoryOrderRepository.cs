using EShopy.Application.Orders;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Orders;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryOrderRepository(InMemoryOrdersState state) : IOrderRepository
{
  public Task<Order?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
  {
    lock (state.Sync)
    {
      var order = state.OrdersByTenant.TryGetValue(tenantId, out var orders)
        ? orders.FirstOrDefault(x => x.Id == id)
        : null;

      return Task.FromResult(order);
    }
  }

  public Task<(IReadOnlyList<Order> Items, long TotalCount)> GetPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    lock (state.Sync)
    {
      var orders = state.OrdersByTenant.TryGetValue(tenantId, out var items)
        ? items.AsEnumerable()
        : Enumerable.Empty<Order>();

      var totalCount = orders.LongCount();
      var pageItems = orders
        .OrderBy(x => x.CreatedAtUtc)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .ToList();

      return Task.FromResult(((IReadOnlyList<Order>)pageItems, totalCount));
    }
  }

  public Task UpdateAsync(Order order, CancellationToken ct) => Task.CompletedTask;
}
