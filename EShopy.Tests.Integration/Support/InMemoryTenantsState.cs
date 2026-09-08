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

  public InMemoryTenantsState()
  {
    var createdAtUtc = DateTime.UnixEpoch;
    var tenantA = CreateActiveTenant("localhost", "Tenant A", createdAtUtc);
    var tenantB = CreateActiveTenant("tenant-b", "Tenant B", createdAtUtc);

    Tenants[tenantA.Id] = tenantA;
    Tenants[tenantB.Id] = tenantB;
    StoresByTenantId[tenantA.Id] = Store.CreateDefault(tenantA.Id, "Tenant A Store", "PYG", createdAtUtc);
    StoresByTenantId[tenantB.Id] = Store.CreateDefault(tenantB.Id, "Tenant B Store", "PYG", createdAtUtc);

    TenantUsers.Add(TenantUser.Create(
      tenantA.Id,
      "test-user-id",
      "test@eshopy.local",
      "Integration Test User",
      TenantUserRole.Admin,
      createdAtUtc));
    TenantUsers.Add(TenantUser.Create(
      tenantB.Id,
      "tenant-b-user-id",
      "tenant-b@eshopy.local",
      "Tenant B User",
      TenantUserRole.Owner,
      createdAtUtc));
  }

  public Guid TenantAId => Tenants.Values.Single(tenant => tenant.Subdomain == "localhost").Id;
  public Guid TenantBId => Tenants.Values.Single(tenant => tenant.Subdomain == "tenant-b").Id;

  private static Tenant CreateActiveTenant(string subdomain, string businessName, DateTime createdAtUtc)
  {
    var tenant = Tenant.Create(subdomain, businessName, TenantPlan.Basic, createdAtUtc);
    tenant.ChangeStatus(TenantStatus.Active, createdAtUtc);
    return tenant;
  }
}
