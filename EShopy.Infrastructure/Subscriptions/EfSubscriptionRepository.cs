using EShopy.Application.Subscriptions;
using EShopy.Domain.Subscriptions;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Subscriptions;

public sealed class EfSubscriptionRepository(EShopyDbContext db) : ISubscriptionRepository
{
  public Task<Subscription?> GetCurrentByTenantIdAsync(Guid tenantId, CancellationToken ct)
    => db.Subscriptions.AsNoTracking()
      .Where(s => s.TenantId == tenantId && s.Status != SubscriptionStatus.Cancelled)
      .FirstOrDefaultAsync(ct);
}
