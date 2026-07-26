using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

/// <summary>
/// Escribe Tenant + Store + TenantUser (Owner) + Subscription en una sola transaccion.
/// Unico punto de la aplicacion que escribe a traves de mas de un agregado: el onboarding
/// de un tenant nuevo es atomico por naturaleza (no puede quedar un Tenant sin su Store).
/// </summary>
public interface ITenantOnboardingWriter
{
  Task CreateAsync(Tenant tenant, Store store, TenantUser owner, Subscription subscription, CancellationToken ct);
}
