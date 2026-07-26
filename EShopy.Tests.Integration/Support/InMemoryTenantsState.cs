using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Tests.Integration.Support;

/// <summary>Estado compartido entre los fakes de Tenants/Store/Subscriptions en tests de integracion.</summary>
internal sealed class InMemoryTenantsState
{
  public readonly Dictionary<Guid, Tenant> Tenants = new();
  public readonly Dictionary<Guid, Store> StoresByTenantId = new();
  public readonly Dictionary<Guid, Subscription> SubscriptionsByTenantId = new();
  public readonly List<TenantUser> TenantUsers = [];
}
