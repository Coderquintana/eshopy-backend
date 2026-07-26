using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Tenants;

public sealed class EfTenantRepository(EShopyDbContext db) : ITenantRepository
{
  public Task<bool> SubdomainExistsAsync(string subdomain, CancellationToken ct)
    => db.Tenants.AsNoTracking().AnyAsync(t => t.Subdomain == subdomain, ct);

  public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct)
    => db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);

  public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct)
    => db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
}
