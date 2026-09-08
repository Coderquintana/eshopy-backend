using EShopy.Domain.Products;
using EShopy.Domain.Tenants;
using EShopy.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace EShopy.Api.DevTools;

/// <summary>
/// Siembra un tenant + tienda + productos de prueba sin pasar por Keycloak, para poder probar el
/// storefront en desarrollo local sin levantar el realm entero. El unico camino documentado para
/// crear un tenant (`POST /api/onboarding/tenants`) crea el owner en Keycloak antes de escribir en
/// la base — sin Keycloak arriba, ese endpoint no sirve para un smoke test rapido.
///
/// Se invoca a mano (`dotnet run --project EShopy.Api -- seed [subdominio]`), nunca automatico: ver
/// el guard de Development en Program.cs.
/// </summary>
public static class DevSeeder
{
  /// <summary>Coincide con el Host que reescribe el proxy de desarrollo del frontend (proxy.conf.json).</summary>
  public const string DefaultSubdomain = "smoketest2";

  public static async Task RunAsync(IServiceProvider services, string subdomain)
  {
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<EShopyDbContext>();

    var existing = await db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain);
    if (existing is not null)
    {
      Log.Information(
        "Ya existe el tenant {Subdomain} ({TenantId}, status={Status}). Nada para sembrar.",
        subdomain, existing.Id, existing.Status);
      return;
    }

    var now = DateTime.UtcNow;

    var tenant = Tenant.Create(subdomain, "Smoke Test Store", TenantPlan.Basic, now);
    tenant.ChangeStatus(TenantStatus.Active, now);
    db.Tenants.Add(tenant);

    var store = Store.CreateDefault(tenant.Id, "Smoke Test Store", "PYG", now);
    store.UpdateProfile(store.Name, store.Timezone,
      primaryColor: "#0f766e", logoUrl: null, backgroundColor: null,
      description: "Tienda de prueba para desarrollo local.", now);
    db.Stores.Add(store);

    var products = new (string Slug, string Name, decimal Price, int Stock)[]
    {
      ("remera-basica", "Remera básica", 85_000m, 20),
      ("gorra-verano", "Gorra de verano", 45_000m, 15),
      ("mochila-urbana", "Mochila urbana", 320_000m, 8),
    };

    foreach (var p in products)
    {
      var product = Product.Create(tenant.Id, store.Id, p.Slug, sku: null, p.Name,
        description: $"{p.Name}, ideal para el día a día.", p.Price, p.Stock, "PYG", now);
      product.ChangeStatus(ProductStatus.Active, now);
      db.Products.Add(product);
    }

    await db.SaveChangesAsync();

    Log.Information("Tenant {Subdomain} creado y activo ({TenantId}).", subdomain, tenant.Id);
    Log.Information("Store {StoreName} ({StoreId}) con {ProductCount} productos publicados.",
      store.Name, store.Id, products.Length);
  }
}
