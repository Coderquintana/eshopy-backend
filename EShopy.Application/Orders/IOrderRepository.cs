using EShopy.Application.Products.Contracts;
using EShopy.Domain.Orders;

namespace EShopy.Application.Orders;

public interface IOrderRepository
{
  Task<Order?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct);

  Task<(IReadOnlyList<Order> Items, long TotalCount)> GetPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct);

  /// <summary>Para transiciones de estado posteriores a la creacion (ej. cancelar un pedido).</summary>
  Task UpdateAsync(Order order, CancellationToken ct);
}
