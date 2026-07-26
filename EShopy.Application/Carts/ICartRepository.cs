using EShopy.Domain.Carts;

namespace EShopy.Application.Carts;

public interface ICartRepository
{
  /// <summary>Trackeado (incluye Items): el caller muta el agregado en memoria y llama SaveChangesAsync.</summary>
  Task<Cart?> GetByCartTokenAsync(Guid tenantId, string cartToken, CancellationToken ct);

  /// <summary>Solo para un Cart nuevo (primera vez que se ve este CartToken). Auto-commit.</summary>
  Task AddAsync(Cart cart, CancellationToken ct);

  /// <summary>Persiste mutaciones sobre un Cart ya trackeado por GetByCartTokenAsync.</summary>
  Task SaveChangesAsync(CancellationToken ct);

  /// <summary>Se usa despues de un checkout exitoso: el carrito ya se convirtio en Order, no tiene sentido dejarlo.</summary>
  Task DeleteAsync(Cart cart, CancellationToken ct);
}
