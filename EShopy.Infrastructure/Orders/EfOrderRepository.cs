using EShopy.Application.Orders;
using EShopy.Application.Products.Contracts;
using EShopy.Domain.Orders;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Orders;

public sealed class EfOrderRepository(EShopyDbContext db) : IOrderRepository
{
  public Task<Order?> GetByIdAsync(Guid tenantId, Guid id, CancellationToken ct)
    => db.Orders.AsNoTracking()
      .Include(o => o.Items)
      .FirstOrDefaultAsync(o => o.TenantId == tenantId && o.Id == id, ct);

  public async Task<(IReadOnlyList<Order> Items, long TotalCount)> GetPagedAsync(Guid tenantId, PagedQuery query, CancellationToken ct)
  {
    var baseQuery = db.Orders.AsNoTracking()
      .Include(o => o.Items)
      .Where(o => o.TenantId == tenantId)
      .OrderByDescending(o => o.CreatedAtUtc);

    var total = await baseQuery.LongCountAsync(ct);
    var items = await baseQuery
      .Skip((query.Page - 1) * query.PageSize)
      .Take(query.PageSize)
      .ToListAsync(ct);

    return (items, total);
  }

  public async Task UpdateAsync(Order order, CancellationToken ct)
  {
    db.Orders.Update(order);
    await db.SaveChangesAsync(ct);
  }
}
