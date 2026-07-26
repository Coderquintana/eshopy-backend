using EShopy.Application.Tenants;
using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;

namespace EShopy.Infrastructure.Tenants;

public sealed class EfTenantActivationWriter(EShopyDbContext db) : ITenantActivationWriter
{
  public async Task ActivateAsync(Tenant tenant, Subscription subscription, CancellationToken ct)
  {
    db.Tenants.Update(tenant);
    db.Subscriptions.Update(subscription);

    await db.SaveChangesAsync(ct);
  }
}
