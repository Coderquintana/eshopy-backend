using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Tenants;

public sealed class EfStoreRepository(EShopyDbContext db) : IStoreRepository
{
  public Task<Store?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
    => db.Stores.AsNoTracking().FirstOrDefaultAsync(s => s.TenantId == tenantId, ct);

  public async Task UpdateAsync(Store store, CancellationToken ct)
  {
    db.Stores.Update(store);
    await db.SaveChangesAsync(ct);
  }
}
