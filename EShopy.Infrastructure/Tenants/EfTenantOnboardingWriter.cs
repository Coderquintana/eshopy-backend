using EShopy.Application.Tenants;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;

namespace EShopy.Infrastructure.Tenants;

public sealed class EfTenantOnboardingWriter(EShopyDbContext db) : ITenantOnboardingWriter
{
  public async Task CreateAsync(Tenant tenant, Store store, TenantUser owner, Subscription subscription, CancellationToken ct)
  {
    db.Tenants.Add(tenant);
    db.Stores.Add(store);
    db.TenantUsers.Add(owner);
    db.Subscriptions.Add(subscription);

    await db.SaveChangesAsync(ct);
  }
}
