using EShopy.Application.Carts;
using EShopy.Domain.Carts;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EShopy.Infrastructure.Carts;

public sealed class EfCartRepository(EShopyDbContext db) : ICartRepository
{
  // Sin AsNoTracking a proposito: el caller muta el agregado (Items incluido) en memoria y
  // despues llama SaveChangesAsync — necesita que EF seguir trackeando los cambios.
  public Task<Cart?> GetByCartTokenAsync(Guid tenantId, string cartToken, CancellationToken ct)
    => db.Carts
      .Include(c => c.Items)
      .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.CartToken == cartToken, ct);

  public async Task AddAsync(Cart cart, CancellationToken ct)
  {
    db.Carts.Add(cart);
    await db.SaveChangesAsync(ct);
  }

  public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

  public async Task DeleteAsync(Cart cart, CancellationToken ct)
  {
    db.Carts.Remove(cart);
    await db.SaveChangesAsync(ct);
  }

  // ExecuteDeleteAsync: DELETE en bloque, sin cargar entidades a memoria. CartItems cascadea a nivel
  // de constraint DB (ON DELETE CASCADE, ver CartConfiguration) — no hace falta borrarlos aparte.
  public Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken ct)
    => db.Carts.Where(c => c.ExpiresAtUtc < nowUtc).ExecuteDeleteAsync(ct);
}
