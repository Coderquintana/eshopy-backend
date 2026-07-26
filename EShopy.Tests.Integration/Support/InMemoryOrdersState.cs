using EShopy.Domain.Orders;
using EShopy.Domain.Payments;

namespace EShopy.Tests.Integration.Support;

/// <summary>Estado compartido entre InMemoryOrderRepository e InMemoryCheckoutWriter — mismo espiritu que InMemoryTenantsState.</summary>
internal sealed class InMemoryOrdersState
{
  public readonly object Sync = new();
  public readonly Dictionary<Guid, List<Order>> OrdersByTenant = new();
  public readonly Dictionary<Guid, Payment> PaymentsById = new();
  public readonly Dictionary<(Guid TenantId, string CounterType), int> Counters = new();
  public readonly HashSet<(string Provider, string EventId)> ProcessedEvents = new();
}
