using EShopy.Application.Tenants;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

internal sealed class InMemoryTenantOnboardingWriter(InMemoryTenantsState state) : ITenantOnboardingWriter
{
  public Task CreateAsync(Tenant tenant, Store store, TenantUser owner, Subscription subscription, CancellationToken ct)
  {
    state.Tenants[tenant.Id] = tenant;
    state.StoresByTenantId[tenant.Id] = store;
    state.SubscriptionsByTenantId[tenant.Id] = subscription;
    state.TenantUsers.Add(owner);
    return Task.CompletedTask;
  }
}
