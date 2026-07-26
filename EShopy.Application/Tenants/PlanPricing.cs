using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

/// <summary>
/// Precio de cada plan al momento de crear una Subscription. GOVERNANCE.md marca los 3 precios
/// como "TBD" (no definidos aun) — se usa 0 en vez de inventar un numero, hasta que el negocio
/// defina precios reales (ver BACKLOG.md).
/// </summary>
internal static class PlanPricing
{
  private const string DefaultCurrencyCode = "PYG";

  internal static (decimal Price, string CurrencyCode) For(TenantPlan plan) => plan switch
  {
    TenantPlan.Basic => (0m, DefaultCurrencyCode),
    TenantPlan.Gold => (0m, DefaultCurrencyCode),
    TenantPlan.Diamond => (0m, DefaultCurrencyCode),
    _ => (0m, DefaultCurrencyCode)
  };
}
