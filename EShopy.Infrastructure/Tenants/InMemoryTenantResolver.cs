using EShopy.Application.Common.Tenants;

namespace EShopy.Infrastructure.Tenants;

public sealed class InMemoryTenantResolver : ITenantResolver
{
  private readonly Dictionary<string, Guid> _tenants = new(StringComparer.OrdinalIgnoreCase)
  {
    // Demo mappings (MVP/skeleton). Reemplazar por consulta a DB/cache.
    ["demo"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
    ["localhost"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
  };

  public Task<Guid?> ResolveTenantIdAsync(string subdomain, CancellationToken ct)
  {
    if (_tenants.TryGetValue(subdomain, out var id))
      return Task.FromResult<Guid?>(id);

    return Task.FromResult<Guid?>(null);
  }
}
