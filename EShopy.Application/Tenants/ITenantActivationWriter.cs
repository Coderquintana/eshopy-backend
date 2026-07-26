using EShopy.Domain.Subscriptions;
using EShopy.Domain.Tenants;

namespace EShopy.Application.Tenants;

/// <summary>
/// Persiste Tenant + Subscription juntos al activar (o, en el futuro, suspender/reactivar) un
/// tenant: ambos deben quedar consistentes o ninguno cambia.
/// </summary>
public interface ITenantActivationWriter
{
  Task ActivateAsync(Tenant tenant, Subscription subscription, CancellationToken ct);
}
