using EShopy.Application.Common.Tenants;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace EShopy.Infrastructure.Tenants;

/// <summary>
/// Resuelve el tenant contra la base de datos, cacheado brevemente en memoria: este middleware
/// corre en cada request, y no queremos pegarle a SQL Server por cada uno.
/// </summary>
public sealed class EfTenantResolver(EShopyDbContext db, IMemoryCache cache) : ITenantResolver
{
  private static readonly TimeSpan FoundTtl = TimeSpan.FromSeconds(60);

  // TTL corto tambien para "no encontrado": evita que un subdominio invalido/typo golpee la DB
  // en cada request, sin cachear negativos por mucho tiempo.
  private static readonly TimeSpan NotFoundTtl = TimeSpan.FromSeconds(30);

  public async Task<TenantResolution?> ResolveAsync(string subdomain, CancellationToken ct)
  {
    var cacheKey = $"tenant-resolution:{subdomain.ToLowerInvariant()}";

    if (cache.TryGetValue(cacheKey, out TenantResolution? cached))
      return cached;

    var tenant = await db.Tenants.AsNoTracking()
      .Where(t => t.Subdomain == subdomain)
      .Select(t => new { t.Id, t.Status })
      .FirstOrDefaultAsync(ct);

    var resolution = tenant is null ? null : new TenantResolution(tenant.Id, tenant.Status);

    cache.Set(cacheKey, resolution, resolution is null ? NotFoundTtl : FoundTtl);

    return resolution;
  }
}
