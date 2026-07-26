using EShopy.Application.Tenants;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryTenantActivationWriter(InMemoryTenantsState state) : ITenantActivationWriter
{
  public Task ActivateAsync(Tenant tenant, Subscription subscription, CancellationToken ct)
  {
    state.Tenants[tenant.Id] = tenant;
    state.SubscriptionsByTenantId[tenant.Id] = subscription;
    return Task.CompletedTask;
  }
}
