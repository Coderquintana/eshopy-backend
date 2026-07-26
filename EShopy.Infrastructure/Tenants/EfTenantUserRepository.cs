using EShopy.Application.Tenants;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Tenants;

public sealed class EfTenantUserRepository(EShopyDbContext db) : ITenantUserRepository
{
  public Task<bool> EmailExistsForTenantAsync(Guid tenantId, string email, CancellationToken ct)
    => db.TenantUsers.AsNoTracking().AnyAsync(u => u.TenantId == tenantId && u.Email == email, ct);

  public async Task<IReadOnlyList<TenantUser>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct)
    => await db.TenantUsers.AsNoTracking()
      .Where(u => u.TenantId == tenantId)
      .OrderBy(u => u.CreatedAtUtc)
      .ToListAsync(ct);

  public async Task AddAsync(TenantUser tenantUser, CancellationToken ct)
  {
    db.TenantUsers.Add(tenantUser);
    await db.SaveChangesAsync(ct);
  }
}
