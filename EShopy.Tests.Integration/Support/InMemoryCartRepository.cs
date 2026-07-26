using EShopy.Application.Carts;
using EShopy.Domain.Carts;

namespace EShopy.Tests.Integration.Support;

/// <summary>
/// Guarda referencias directas al Cart, no copias — mutar el agregado ya "trackea" el cambio,
/// SaveChangesAsync es un no-op. Mismo espiritu que InMemoryProductRepository.
/// </summary>
internal sealed class InMemoryCartRepository : ICartRepository
{
  private readonly object _sync = new();
  private readonly Dictionary<(Guid TenantId, string CartToken), Cart> _carts = new();

  public Task<Cart?> GetByCartTokenAsync(Guid tenantId, string cartToken, CancellationToken ct)
  {
    lock (_sync)
    {
      _carts.TryGetValue((tenantId, cartToken), out var cart);
      return Task.FromResult(cart);
    }
  }

  public Task AddAsync(Cart cart, CancellationToken ct)
  {
    lock (_sync)
    {
      _carts[(cart.TenantId, cart.CartToken)] = cart;
    }
    return Task.CompletedTask;
  }

  public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

  public Task DeleteAsync(Cart cart, CancellationToken ct)
  {
    lock (_sync)
    {
      _carts.Remove((cart.TenantId, cart.CartToken));
    }
    return Task.CompletedTask;
  }

  public Task<int> DeleteExpiredAsync(DateTime nowUtc, CancellationToken ct)
  {
    lock (_sync)
    {
      var expiredKeys = _carts.Where(kv => kv.Value.ExpiresAtUtc < nowUtc).Select(kv => kv.Key).ToList();
      foreach (var key in expiredKeys)
        _carts.Remove(key);

      return Task.FromResult(expiredKeys.Count);
    }
  }
}
