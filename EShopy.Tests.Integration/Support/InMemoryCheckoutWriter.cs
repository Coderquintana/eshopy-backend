using EShopy.Application.Orders;
using EShopy.Domain.Common.Counters;
using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Tests.Integration.Support;

/// <summary>
/// Simula la asignacion atomica de OrderNumber con un lock en memoria en vez del concurrency-token
/// EF real (ver EfCheckoutWriter) — suficiente para probar la orquestacion del handler; la garantia
/// de atomicidad real se prueba contra SQL Server real (ver domain/orders.md "Escritura atomica").
/// </summary>
internal sealed class InMemoryCheckoutWriter(InMemoryOrdersState state) : ICheckoutWriter
{
  public Task<int> CreateAsync(Order order, Payment payment, CancellationToken ct)
  {
    lock (state.Sync)
    {
      var counterKey = (order.TenantId, TenantCounter.OrderNumberType);
      var nextValue = state.Counters.GetValueOrDefault(counterKey) + 1;
      state.Counters[counterKey] = nextValue;

      order.AssignOrderNumber(nextValue);

      if (!state.OrdersByTenant.TryGetValue(order.TenantId, out var orders))
      {
        orders = [];
        state.OrdersByTenant[order.TenantId] = orders;
      }

      orders.Add(order);
      state.PaymentsById[payment.Id] = payment;

      return Task.FromResult(nextValue);
    }
  }
}
