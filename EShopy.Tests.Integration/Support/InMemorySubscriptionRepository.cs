using EShopy.Application.Subscriptions;
using EShopy.Domain.Subscriptions;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemorySubscriptionRepository(InMemoryTenantsState state) : ISubscriptionRepository
{
  public Task<Subscription?> GetCurrentByTenantIdAsync(Guid tenantId, CancellationToken ct)
  {
    var subscription = state.SubscriptionsByTenantId.GetValueOrDefault(tenantId);
    return Task.FromResult(subscription is { Status: not SubscriptionStatus.Cancelled } ? subscription : null);
  }
}
